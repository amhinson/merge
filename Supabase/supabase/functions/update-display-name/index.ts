import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders, getAuthenticatedUserId } from "../_shared/auth.ts";
import { containsProfanity } from "../_shared/profanity.ts";

const MAX_NAME_CHANGES_PER_DAY = 3;

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const body = await req.json();

    const { userId, error: authError } = await getAuthenticatedUserId(req, body);
    if (authError) return authError;

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    const { display_name } = body;

    if (!display_name) {
      return new Response(
        JSON.stringify({ error: "display_name required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Validate name: 3-24 chars, alphanumeric + spaces/underscores/hyphens
    const sanitized = display_name.replace(/[^a-zA-Z0-9 _\-]/g, "").trim();
    if (sanitized.length < 3 || sanitized.length > 24) {
      return new Response(
        JSON.stringify({ error: "Name must be 3-24 alphanumeric characters" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    if (containsProfanity(sanitized)) {
      return new Response(
        JSON.stringify({ error: "Name contains inappropriate language" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Check daily name change limit
    const { data: player } = await supabase
      .from("players")
      .select("display_name, name_changes_today, name_change_date")
      .eq("user_id", userId)
      .single();

    if (player) {
      const today = new Date().toISOString().split("T")[0];
      const changestoday = player.name_change_date === today ? (player.name_changes_today || 0) : 0;

      if (changestoday >= MAX_NAME_CHANGES_PER_DAY) {
        return new Response(
          JSON.stringify({ error: `Max ${MAX_NAME_CHANGES_PER_DAY} name changes per day` }),
          { status: 429, headers: { ...corsHeaders, "Content-Type": "application/json" } }
        );
      }

      // Skip if name hasn't changed
      if (player.display_name === sanitized) {
        return new Response(
          JSON.stringify({ success: true, display_name: sanitized }),
          { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
        );
      }

      // Update player name + increment counter
      const { error } = await supabase
        .from("players")
        .update({
          display_name: sanitized,
          name_changes_today: changestoday + 1,
          name_change_date: today,
        })
        .eq("user_id", userId);

      if (error) {
        return new Response(
          JSON.stringify({ error: error.message }),
          { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
        );
      }
    }

    // Update display_name in today's daily_scores only (not historical)
    const today = new Date().toISOString().split("T")[0];
    await supabase
      .from("daily_scores")
      .update({ display_name: sanitized })
      .eq("user_id", userId)
      .eq("game_date", today);

    return new Response(
      JSON.stringify({ success: true, display_name: sanitized }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
