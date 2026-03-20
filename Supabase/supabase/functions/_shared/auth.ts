export const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

/**
 * Verify the apikey header matches the publishable key.
 * Since we deploy with --no-verify-jwt, we validate the key ourselves.
 */
export function verifyApiKey(req: Request): Response | null {
  const apikey = req.headers.get("apikey");
  const expectedKey = Deno.env.get("APP_PUBLISHABLE_KEY");

  if (!expectedKey) {
    // If env var not set, skip verification (dev mode)
    return null;
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

  return null; // Authorized
}
