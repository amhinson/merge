import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    const { device_uuid, display_name, score, game_date, day_number, largest_merges } =
      await req.json();

    if (!device_uuid || !game_date || score == null) {
      return new Response(
        JSON.stringify({ error: "Missing required fields" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Upsert player record
    const { error: playerError } = await supabase
      .from("players")
      .upsert(
        { device_uuid, display_name: display_name || "Player" },
        { onConflict: "device_uuid" }
      );

    if (playerError) {
      return new Response(
        JSON.stringify({ error: "Failed to upsert player", details: playerError.message }),
        { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Check if score already exists for this UUID + date
    const { data: existing } = await supabase
      .from("daily_scores")
      .select("id")
      .eq("device_uuid", device_uuid)
      .eq("game_date", game_date)
      .single();

    if (existing) {
      return new Response(
        JSON.stringify({ error: "Score already submitted for this date" }),
        { status: 409, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Insert score
    const { error: scoreError } = await supabase.from("daily_scores").insert({
      device_uuid,
      game_date,
      score,
      day_number: day_number || 1,
      largest_merges: largest_merges || [],
    });

    if (scoreError) {
      return new Response(
        JSON.stringify({ error: "Failed to insert score", details: scoreError.message }),
        { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    return new Response(
      JSON.stringify({ success: true }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
