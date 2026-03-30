import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders, getAuthenticatedUserId } from "../_shared/auth.ts";

/**
 * Migrate player data from an anonymous user_id to a provider-linked user_id.
 * Copies display_name and streak data, then transfers any daily_scores.
 * The old anonymous player row is left in place (orphaned).
 */
serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const body = await req.json();
    const { userId: authUserId, error: authError } = await getAuthenticatedUserId(req, body);
    if (authError) return authError;

    const fromUserId = body.from_user_id as string;
    const toUserId = body.to_user_id as string;

    if (!fromUserId || !toUserId) {
      return new Response(
        JSON.stringify({ error: "from_user_id and to_user_id required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Verify the authenticated user is the target (to_user_id)
    if (authUserId !== toUserId) {
      return new Response(
        JSON.stringify({ error: "Can only migrate to your own account" }),
        { status: 403, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    // 1. Get the old player's data
    const { data: oldPlayer } = await supabase
      .from("players")
      .select("*")
      .eq("user_id", fromUserId)
      .single();

    if (!oldPlayer) {
      return new Response(
        JSON.stringify({ error: "Source player not found", migrated: false }),
        { status: 404, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // 2. Check if the target player already exists
    const { data: existingPlayer } = await supabase
      .from("players")
      .select("user_id")
      .eq("user_id", toUserId)
      .single();

    if (!existingPlayer) {
      // Target doesn't exist — create with old player's data
      const { error: insertError } = await supabase
        .from("players")
        .insert({
          user_id: toUserId,
          display_name: oldPlayer.display_name,
          current_streak: oldPlayer.current_streak,
          longest_streak: oldPlayer.longest_streak,
          name_changes_today: oldPlayer.name_changes_today,
          name_change_date: oldPlayer.name_change_date,
        });
      if (insertError) {
        return new Response(
          JSON.stringify({ error: insertError.message }),
          { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
        );
      }
    }
    // If target exists, keep their existing data (don't overwrite)

    // 3. Transfer daily_scores — only migrate dates that don't conflict
    // First, get dates that already have scores under the new user
    const { data: existingDates } = await supabase
      .from("daily_scores")
      .select("game_date")
      .eq("user_id", toUserId);

    const existingDateSet = new Set((existingDates || []).map((d: any) => d.game_date));

    // Get all scores from old user
    const { data: oldScores } = await supabase
      .from("daily_scores")
      .select("id, game_date")
      .eq("user_id", fromUserId);

    // Transfer non-conflicting scores
    const toMigrate = (oldScores || []).filter((s: any) => !existingDateSet.has(s.game_date));
    let migratedScores = 0;

    if (toMigrate.length > 0) {
      const ids = toMigrate.map((s: any) => s.id);
      const { data: migrated } = await supabase
        .from("daily_scores")
        .update({ user_id: toUserId })
        .in("id", ids)
        .select("id");
      migratedScores = migrated?.length ?? 0;
    }

    // Delete any remaining old scores (conflicts — keep the provider account's version)
    await supabase
      .from("daily_scores")
      .delete()
      .eq("user_id", fromUserId);

    // 4. Clean up — delete orphaned anonymous player row
    await supabase
      .from("players")
      .delete()
      .eq("user_id", fromUserId);

    // 5. Delete the anonymous auth user
    await supabase.auth.admin.deleteUser(fromUserId);

    return new Response(
      JSON.stringify({
        success: true,
        migrated_scores: migratedScores,
        display_name: oldPlayer.display_name,
      }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
