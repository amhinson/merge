import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

export const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

/**
 * Get the authenticated user ID from the request via Supabase JWT.
 * Requires Authorization: Bearer <token> header.
 */
export async function getAuthenticatedUserId(
  req: Request,
  _body?: Record<string, unknown>
): Promise<{ userId: string | null; error: Response | null }> {
  const authHeader = req.headers.get("authorization");
  if (!authHeader?.startsWith("Bearer ")) {
    return {
      userId: null,
      error: new Response(
        JSON.stringify({ error: "Missing or invalid Authorization header" }),
        {
          status: 401,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        }
      ),
    };
  }

  const token = authHeader.slice(7);
  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? "";
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";

  const supabase = createClient(supabaseUrl, serviceKey);
  const {
    data: { user },
    error,
  } = await supabase.auth.getUser(token);

  if (error || !user) {
    return {
      userId: null,
      error: new Response(
        JSON.stringify({ error: "Invalid or expired token" }),
        {
          status: 401,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        }
      ),
    };
  }

  return { userId: user.id, error: null };
}

/**
 * Verify the apikey header matches the publishable key.
 * Used only for public endpoints (e.g., get-leaderboard).
 */
export function verifyApiKey(req: Request): Response | null {
  const apikey = req.headers.get("apikey");
  const expectedKey = Deno.env.get("APP_PUBLISHABLE_KEY");

  if (!expectedKey) {
    return null; // dev mode — skip verification
  }

  if (apikey !== expectedKey) {
    return new Response(
      JSON.stringify({ error: "Invalid API key" }),
      {
        status: 401,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      }
    );
  }

  return null;
}
