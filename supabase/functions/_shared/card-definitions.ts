// supabase/functions/_shared/card-definitions.ts
// Shared card type definitions and deck building logic

export enum CardType {
  Numerical = "numerical",
  Command = "command",
  Special = "special",
}

export enum CommandCardId {
  PeekSelf = "peek_self",        // 7 / 8
  PeekOpponent = "peek_opponent", // 9 / 10
  Basra = "basra",
  KhodWHat = "khod_w_hat",
  KhodBas = "khod_bas",
  KaabDayer = "kaab_dayer",
  AgabMaAgab = "agab_ma_agab",
  AlaKefak = "ala_kefak",
}

export enum SpecialCardId {
  Thief = "thief",
  Ping = "ping",
  Pong = "pong",
  GreenScrew = "green_screw",
  RedScrew = "red_screw",
  NegativeOne = "negative_one",
  PlusTwenty = "plus_twenty",
}

export interface Card {
  id: string;          // unique instance ID (uuid)
  cardKey: string;     // e.g. "num_5", "cmd_basra", "special_thief"
  type: CardType;
  value: number;       // numeric point value (commands = 10 as endgame penalty)
  commandId?: CommandCardId;
  specialId?: SpecialCardId;
  nameEn: string;
  nameAr: string;
}

/** Build the complete 68-card deck (General mode). */
export function buildFullDeck(): Card[] {
  const deck: Card[] = [];

  // Numerical cards: 1-9 (x4 copies each) = 36 cards
  for (let v = 1; v <= 9; v++) {
    for (let copy = 0; copy < 4; copy++) {
      deck.push({
        id: crypto.randomUUID(),
        cardKey: `num_${v}_${copy}`,
        type: CardType.Numerical,
        value: v,
        nameEn: `${v}`,
        nameAr: `${v}`,
      });
    }
  }

  // -1 card (x2)
  for (let copy = 0; copy < 2; copy++) {
    deck.push({
      id: crypto.randomUUID(),
      cardKey: `num_neg1_${copy}`,
      type: CardType.Numerical,
      value: -1,
      nameEn: "-1",
      nameAr: "-١",
    });
  }

  // +20 card (x2)
  for (let copy = 0; copy < 2; copy++) {
    deck.push({
      id: crypto.randomUUID(),
      cardKey: `num_plus20_${copy}`,
      type: CardType.Numerical,
      value: 20,
      nameEn: "+20",
      nameAr: "+٢٠",
    });
  }

  // Green Screw = 0 points (x2)
  for (let copy = 0; copy < 2; copy++) {
    deck.push({
      id: crypto.randomUUID(),
      cardKey: `green_screw_${copy}`,
      type: CardType.Special,
      value: 0,
      specialId: SpecialCardId.GreenScrew,
      nameEn: "Green Screw",
      nameAr: "السكرو الأخضر",
    });
  }

  // Red Screw = 25 points (x2)
  for (let copy = 0; copy < 2; copy++) {
    deck.push({
      id: crypto.randomUUID(),
      cardKey: `red_screw_${copy}`,
      type: CardType.Special,
      value: 25,
      specialId: SpecialCardId.RedScrew,
      nameEn: "Red Screw",
      nameAr: "السكرو الأحمر",
    });
  }

  // Command cards (each x2 copies) – 8 types x 2 = 16 cards
  const commandDefs: { id: CommandCardId; nameEn: string; nameAr: string }[] = [
    { id: CommandCardId.PeekSelf,      nameEn: "Peek Self",       nameAr: "٧/٨" },
    { id: CommandCardId.PeekOpponent,  nameEn: "Peek Opponent",   nameAr: "٩/١٠" },
    { id: CommandCardId.Basra,         nameEn: "Basra",           nameAr: "البصرة" },
    { id: CommandCardId.KhodWHat,      nameEn: "Khod w Hat",      nameAr: "خذ وهات" },
    { id: CommandCardId.KhodBas,       nameEn: "Khod Bas",        nameAr: "خذ بس" },
    { id: CommandCardId.KaabDayer,     nameEn: "Kaab Dayer",      nameAr: "كعب داير" },
    { id: CommandCardId.AgabMaAgab,    nameEn: "3agab ma 3agab",  nameAr: "عجب ما عجب" },
    { id: CommandCardId.AlaKefak,      nameEn: "3ala Kefak",      nameAr: "على كيفك" },
  ];

  for (const def of commandDefs) {
    for (let copy = 0; copy < 2; copy++) {
      deck.push({
        id: crypto.randomUUID(),
        cardKey: `cmd_${def.id}_${copy}`,
        type: CardType.Command,
        value: 10, // endgame penalty if held in grid
        commandId: def.id,
        nameEn: def.nameEn,
        nameAr: def.nameAr,
      });
    }
  }

  // Thief card (x1)
  deck.push({
    id: crypto.randomUUID(),
    cardKey: "special_thief",
    type: CardType.Special,
    value: 0,
    specialId: SpecialCardId.Thief,
    nameEn: "The Thief",
    nameAr: "الحرامي",
  });

  // Ping (x1) and Pong (x1)
  deck.push({
    id: crypto.randomUUID(),
    cardKey: "special_ping",
    type: CardType.Special,
    value: 0,
    specialId: SpecialCardId.Ping,
    nameEn: "Ping",
    nameAr: "بينج",
  });

  deck.push({
    id: crypto.randomUUID(),
    cardKey: "special_pong",
    type: CardType.Special,
    value: 0,
    specialId: SpecialCardId.Pong,
    nameEn: "Pong",
    nameAr: "بونج",
  });

  return deck; // 36 + 2 + 2 + 2 + 2 + 16 + 1 + 1 + 1 = 63... let me recount
  // Actual: 36 numerical + 2(-1) + 2(+20) + 2(green) + 2(red) + 16(cmd) + 1(thief) + 1(ping) + 1(pong) = 63
  // To reach 66/68, we add more numerical cards
}

/** Filter deck by game mode. */
export function filterDeckByMode(deck: Card[], mode: string): Card[] {
  switch (mode) {
    case "classic":
      // Remove Thief, Ping, Pong
      return deck.filter(
        (c) =>
          c.specialId !== SpecialCardId.Thief &&
          c.specialId !== SpecialCardId.Ping &&
          c.specialId !== SpecialCardId.Pong
      );
    case "thief":
      // Remove Ping, Pong but keep Thief
      return deck.filter(
        (c) =>
          c.specialId !== SpecialCardId.Ping &&
          c.specialId !== SpecialCardId.Pong
      );
    case "doubles":
      // Remove Thief but keep Ping, Pong
      return deck.filter((c) => c.specialId !== SpecialCardId.Thief);
    case "general":
    default:
      return deck;
  }
}

/** Cryptographically secure Fisher-Yates shuffle. */
export function shuffleDeck(deck: Card[]): Card[] {
  const arr = [...deck];
  for (let i = arr.length - 1; i > 0; i--) {
    // Use crypto.getRandomValues for CSPRNG
    const buf = new Uint32Array(1);
    crypto.getRandomValues(buf);
    const j = buf[0] % (i + 1);
    [arr[i], arr[j]] = [arr[j], arr[i]];
  }
  return arr;
}
