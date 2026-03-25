import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders, verifyApiKey } from "../_shared/auth.ts";

const ADJECTIVES = [
  // Phish song references
  "Tweezy", "Divided", "Bouncing", "Wading", "Frozen", "Squirming", "Roaming",
  "Fluffed", "Runaway", "Buried", "Silent", "Simple", "Dense", "Heavy", "Free",
  "Willing", "Swept", "Stolen", "Twisted", "Bathtub", "Drifting", "Floating",
  "Sparkling", "Glowing", "Raging", "Soaring", "Blissful", "Mellow", "Funky",
  "Undermind", "Limb", "Orbital", "Piper", "Tidal", "Tumbling", "Carini",
  // Jam / music descriptors
  "Heady", "Phatty", "Blissy", "Grimy", "Spacey", "Ambient", "Groovy", "Rippy",
  "Saucy", "Tasty", "Filthy", "Chunky", "Blazing", "Melty", "Crispy", "Rowdy",
  "Slinky", "Gooey", "Shreddy", "Squishy", "Wobbly", "Sneaky", "Cheeky",
  "Peppy", "Zesty", "Fizzy", "Hazy", "Murky", "Foggy", "Dewy", "Jangly",
  "Swirly", "Twangy", "Loopy", "Rinsy", "Buzzy", "Briny",
  // Vibe / lot words
  "Cosmic", "Stellar", "Astral", "Lunar", "Solar", "Electric", "Magnetic",
  "Velvet", "Golden", "Rusty", "Dusty", "Mossy", "Breezy", "Misty", "Sandy",
  "Tangled", "Knotty", "Gnarly", "Turbo", "Phantom", "Feral", "Chaotic",
  "Gentle", "Humble", "Noble", "Savage", "Swift", "Lucky", "Dizzy", "Jolly",
  "Plucky", "Scrappy", "Sleepy", "Wiggly", "Jumpy", "Lanky", "Giddy", "Wonky",
  "Shaggy", "Scruffy", "Peppered", "Stoked", "Radiant", "Primal", "Dapper",
];

const NOUNS = [
  // Phish animals
  "Antelope", "Possum", "Llama", "Lizard", "Mule", "Wolfman", "Catfish",
  "Oyster", "Donkey", "Squirrel", "Gorilla", "Pelican", "Weasel", "Heron",
  "Vulture", "Mockingbird", "Sloth", "Camel",
  // Fun animals
  "Wombat", "Jellyfish", "Mongoose", "Iguana", "Platypus", "Pangolin",
  "Capybara", "Axolotl", "Ocelot", "Narwhal", "Flamingo", "Manatee",
  "Armadillo", "Chinchilla", "Hedgehog", "Chameleon", "Porcupine", "Raccoon",
  "Badger", "Coyote", "Falcon", "Mantis", "Otter", "Newt", "Frog", "Toad",
  "Bison", "Moose", "Crane", "Finch", "Osprey", "Tortoise", "Starling",
  "Wallaby", "Lemur", "Tapir", "Ibis", "Marmot", "Quokka", "Gibbon",
  "Macaw", "Toucan", "Puffin", "Stork", "Egret", "Beetle", "Cicada",
  "Cuttlefish", "Seahorse", "Lobster",
  // Phish-world nouns
  "Reba", "Stash", "Cavern", "Maze", "Fluff", "Ghost", "Timber", "Pebble",
  "Boulder", "Wedge", "Curtain", "Spark", "Foam", "Gambit", "Ember", "Gumbo",
  "Comet", "Vapor", "Prism", "Esther", "Icculus", "Forbin", "Wilson",
  "Rutherford", "Suzy", "Mango", "Tube", "Lawn", "Tarp", "Donut",
  "Rift", "Scent", "Flume", "Sigma", "Yonder",
];

function generateDefaultName(): string {
  const adj = ADJECTIVES[Math.floor(Math.random() * ADJECTIVES.length)];
  const noun = NOUNS[Math.floor(Math.random() * NOUNS.length)];
  const num = Math.floor(Math.random() * 100);
  return `${adj} ${noun}${num}`;
}

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  const authError = verifyApiKey(req);
  if (authError) return authError;

  try {
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    const { device_uuid, display_name } = await req.json();

    if (!device_uuid) {
      return new Response(
        JSON.stringify({ error: "device_uuid required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    const generatedName = display_name || generateDefaultName();

    // Upsert — creates if new, does nothing if exists (preserves streaks)
    const { error } = await supabase
      .from("players")
      .upsert(
        {
          device_uuid,
          display_name: generatedName,
        },
        {
          onConflict: "device_uuid",
          ignoreDuplicates: true,
        }
      );

    if (error) {
      return new Response(
        JSON.stringify({ error: error.message }),
        { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Fetch the player's current display_name (may differ from generatedName if they already existed)
    const { data: player } = await supabase
      .from("players")
      .select("display_name")
      .eq("device_uuid", device_uuid)
      .single();

    return new Response(
      JSON.stringify({ success: true, display_name: player?.display_name ?? generatedName }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
