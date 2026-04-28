// supabase/functions/_shared/supabase-admin.ts
// Shared Supabase admin client for Edge Functions

import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

export function getAdminClient() {
  const url = Deno.env.get("SUPABASE_URL")!;
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
  return createClient(url, serviceKey, {
    auth: {
      autoRefreshToken: false,
      persistSession: false,
    },
  });
}

/** Return 400 Bad Request JSON response */
export function badRequest(message: string): Response {
  return new Response(JSON.stringify({ error: message }), {
    status: 400,
    headers: { "Content-Type": "application/json" },
  });
}

/** Return 200 OK JSON response */
export function ok(data: unknown): Response {
  return new Response(JSON.stringify(data), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

/** Return 500 Internal Server Error JSON response */
export function internalError(message: string): Response {
  return new Response(JSON.stringify({ error: message }), {
    status: 500,
    headers: { "Content-Type": "application/json" },
  });
}
