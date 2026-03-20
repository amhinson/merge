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

    const url = new URL(req.url);
    const game_date = url.searchParams.get("game_date");

    if (!game_date) {
      return new Response(
        JSON.stringify({ error: "game_date parameter required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    const { data, error } = await supabase
      .from("daily_scores")
      .select(
        `
        device_uuid,
        score,
        largest_merges,
        players!inner(display_name)
      `
      )
      .eq("game_date", game_date)
      .order("score", { ascending: false })
      .limit(100);

    if (error) {
      return new Response(
        JSON.stringify({ error: error.message }),
        { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Flatten the joined data
    const entries = (data || []).map((row: any, index: number) => ({
      device_uuid: row.device_uuid,
      display_name: row.players?.display_name || "Player",
      score: row.score,
      largest_merges: row.largest_merges || [],
      rank: index + 1,
    }));

    return new Response(JSON.stringify(entries), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
