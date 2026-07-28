# Global Economy Foundation (Meta 8)

## Trust boundary

Unity is a requester, never the authority for wallet, sensitive inventory, or
global market state. The client sends a command with a unique request ID and
waits for the response before changing the UI. `FarmMockGlobalEconomyService`
implements this same asynchronous contract for local development.

`FarmSupabaseEconomyService` makes authenticated calls to the `farm-economy`
Edge Function. It may use a Supabase publishable key and a signed user JWT, but
it must never contain a secret/service-role key. The actual account-to-Steam
identity link is intentionally deferred until Steam authentication is chosen.

## Server contract

The Edge Function accepts an operation plus JSON payload:

- `account.get`
- `market.list`
- `market.create` (`RequestId`, `ItemId`, `Quantity`, `UnitPrice`)
- `market.buy` (`RequestId`, `OrderId`, `Quantity`)

The database migrations at `Backend/Supabase/migrations/` create protected
tables, RLS read policies, and transactional RPCs. A purchase locks the order
and affected account rows, verifies current quantity and funds, transfers the
escrowed item and currency exactly once, writes a command receipt, and returns
the same receipt if the client retries the request.

## Required before live deployment

1. Create the Supabase project and configure authenticated user sign-in.
2. Apply the migrations in order, review every RLS policy, and run concurrent-buy tests.
3. Deploy and test the Edge Function against a non-production project.
4. Set `FarmSupabaseSettings` with the project URL and **publishable** key.
5. Implement an `IFarmAccessTokenProvider`; it supplies a user JWT at runtime.
6. Keep service keys exclusively in Supabase function secrets.

Supabase's current guidance is to keep JWT verification enabled for user-facing
Edge Functions and use RLS on exposed tables. See [Edge Function auth](https://supabase.com/docs/guides/functions/auth)
and [Row Level Security](https://supabase.com/docs/guides/database/postgres/row-level-security).
