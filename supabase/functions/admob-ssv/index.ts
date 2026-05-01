// supabase/functions/admob-ssv/index.ts
// Edge Function: Google AdMob Server-Side Verification (SSV) callback handler.
// Verifies ECDSA signature from Google and awards coins to the verified user.

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { getAdminClient, ok, internalError } from "../_shared/supabase-admin.ts";

const GOOGLE_SSV_KEY_URL = "https://gstatic.com/admob/reward/verifier-keys.json";

// Cache Google public keys to avoid fetching on every request
let googleKeysCache: Record<string, string> | null = null;
let keyCacheExpiry = 0;

async function getGooglePublicKeys(): Promise<Record<string, string>> {
  const now = Date.now();
  if (googleKeysCache && now < keyCacheExpiry) {
    return googleKeysCache;
  }

  const res = await fetch(GOOGLE_SSV_KEY_URL);
  if (!res.ok) throw new Error("Failed to fetch Google AdMob keys");

  const data = await res.json();
  googleKeysCache = data.keys.reduce((acc: Record<string, string>, key: any) => {
    acc[key.keyId] = key.pem;
    return acc;
  }, {});

  // Cache for 1 hour
  keyCacheExpiry = now + 60 * 60 * 1000;
  return googleKeysCache!;
}

async function verifyEcdsaSignature(
  message: string,
  signatureBase64Url: string,
  pemPublicKey: string
): Promise<boolean> {
  try {
    // Import PEM key
    const pemHeader = "-----BEGIN PUBLIC KEY-----";
    const pemFooter = "-----END PUBLIC KEY-----";
    const pemContents = pemPublicKey
      .replace(pemHeader, "")
      .replace(pemFooter, "")
      .replace(/\s/g, "");

    const binaryKey = Uint8Array.from(atob(pemContents), (c) => c.charCodeAt(0));

    const cryptoKey = await crypto.subtle.importKey(
      "spki",
      binaryKey.buffer,
      { name: "ECDSA", namedCurve: "P-256" },
      false,
      ["verify"]
    );

    // Decode base64url signature
    const base64 = signatureBase64Url.replace(/-/g, "+").replace(/_/g, "/");
    const sigBytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));

    const msgBytes = new TextEncoder().encode(message);

    return await crypto.subtle.verify(
      { name: "ECDSA", hash: "SHA-256" },
      cryptoKey,
      sigBytes.buffer,
      msgBytes.buffer
    );
  } catch (err) {
    console.error("[admob-ssv] Signature verification error:", err);
    return false;
  }
}

serve(async (req: Request) => {
  if (req.method !== "GET") {
    return new Response("Method not allowed", { status: 405 });
  }

  const url = new URL(req.url);
  const params = url.searchParams;

  const keyId = params.get("key_id");
  const signature = params.get("signature");
  const customData = params.get("custom_data"); // contains user_id
  const rewardAmount = params.get("reward_amount");
  const rewardItem = params.get("reward_item");
  const transactionId = params.get("transaction_id");

  if (!keyId || !signature || !customData || !rewardAmount) {
    return new Response("Missing required parameters", { status: 400 });
  }

  try {
    // Reconstruct the signed query string (everything except the signature itself)
    // Google signs: all params sorted alphabetically EXCLUDING 'signature'
    const signedParamKeys = Array.from(params.keys())
      .filter((k) => k !== "signature")
      .sort();

    const signedString = signedParamKeys
      .map((k) => `${k}=${params.get(k)}`)
      .join("&");

    // Fetch Google's public keys
    const keys = await getGooglePublicKeys();
    const pemKey = keys[keyId];

    if (!pemKey) {
      console.error(`[admob-ssv] Unknown key_id: ${keyId}`);
      return new Response("Unknown key_id", { status: 400 });
    }

    // Verify ECDSA signature
    const isValid = await verifyEcdsaSignature(signedString, signature, pemKey);

    if (!isValid) {
      console.error("[admob-ssv] Invalid signature – potential spoofing attempt");
      return new Response("Invalid signature", { status: 403 });
    }

    // Parse user_id from custom_data (expected JSON: { "user_id": "..." })
    let userId: string;
    try {
      const parsed = JSON.parse(customData);
      userId = parsed.user_id;
    } catch {
      return new Response("Invalid custom_data format", { status: 400 });
    }

    if (!userId) {
      return new Response("Missing user_id in custom_data", { status: 400 });
    }

    const coinsToAward = Math.max(0, parseInt(rewardAmount, 10) || 0);

    // Award coins via service-role admin update
    const supabase = getAdminClient();

    // Use a transaction-safe increment via RPC
    const { error: rpcError } = await supabase.rpc("award_coins", {
      p_user_id: userId,
      p_coins: coinsToAward,
      p_transaction_id: transactionId ?? crypto.randomUUID(),
      p_source: "admob_rewarded_ad",
    });

    if (rpcError) {
      console.error("[admob-ssv] Failed to award coins:", rpcError.message);
      return internalError("Failed to award reward");
    }

    console.log(`[admob-ssv] Awarded ${coinsToAward} coins to user ${userId}`);
    return ok({ success: true, coins_awarded: coinsToAward });
  } catch (err) {
    console.error("[admob-ssv] Unexpected error:", err);
    return internalError("Unexpected server error");
  }
});
