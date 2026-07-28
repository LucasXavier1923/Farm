// Contract scaffold for Meta 8. Deploy only after the SQL RPCs are implemented
// and tested in a real Supabase project. This file deliberately contains no
// secret key; Supabase injects server-side credentials at deployment/runtime.
import { withSupabase } from "npm:@supabase/server";

type Operation = "account.get" | "market.list" | "market.create" | "market.buy";

export default {
  fetch: withSupabase({ auth: "user" }, async (request, ctx) => {
    const body = await request.json() as { Operation?: Operation; Payload?: string };
    const operation = body.Operation;
    const payload = body.Payload ? JSON.parse(body.Payload) : {};

    if (operation === "account.get") {
      const { data, error } = await ctx.supabase.rpc("farm_get_account_snapshot");
      return Response.json(error ? { Succeeded: false, Message: error.message } : data, { status: error ? 400 : 200 });
    }
    if (operation === "market.list") {
      const { data, error } = await ctx.supabase.rpc("farm_list_active_market_orders");
      return Response.json(error ? { Orders: [], Message: error.message } : data, { status: error ? 400 : 200 });
    }
    if (operation === "market.create" || operation === "market.buy") {
      const { data, error } = await ctx.supabase.rpc(
        operation === "market.create" ? "farm_create_market_order" : "farm_buy_market_order",
        { p_request: payload },
      );
      return Response.json(error ? { Succeeded: false, Message: error.message } : data, { status: error ? 400 : 200 });
    }
    return Response.json({ Succeeded: false, Message: "Unknown economy operation." }, { status: 400 });
  }),
};
