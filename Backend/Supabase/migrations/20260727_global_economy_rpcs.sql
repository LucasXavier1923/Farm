-- Meta 8 transactional command handlers. Apply after 20260726_global_economy_foundation.sql.
-- Every function executes as the signed-in caller (auth.uid()) and uses a request
-- receipt for idempotency. Keep the schema-qualified search_path fixed.

create or replace function public.farm_ensure_account(p_user_id uuid)
returns void language plpgsql security definer set search_path = public, pg_temp as $$
begin
  insert into public.farm_accounts (user_id) values (p_user_id) on conflict (user_id) do nothing;
end;
$$;

create or replace function public.farm_get_account_snapshot()
returns jsonb language plpgsql security definer set search_path = public, pg_temp as $$
declare v_user_id uuid := auth.uid();
begin
  if v_user_id is null then raise exception 'Authentication required.'; end if;
  perform public.farm_ensure_account(v_user_id);
  return jsonb_build_object(
    'Succeeded', true,
    'Message', 'Account loaded.',
    'Account', (
      select jsonb_build_object(
        'AccountId', a.user_id::text, 'Revision', a.revision, 'Wallet', a.wallet,
        'Inventory', coalesce((select jsonb_agg(jsonb_build_object('ItemId', i.item_id, 'Quantity', i.quantity, 'Quality', 0) order by i.item_id)
                               from public.farm_inventory i where i.user_id = a.user_id and i.quantity > 0), '[]'::jsonb)
      ) from public.farm_accounts a where a.user_id = v_user_id
    )
  );
end;
$$;

create or replace function public.farm_list_active_market_orders()
returns jsonb language plpgsql security definer set search_path = public, pg_temp as $$
declare v_user_id uuid := auth.uid();
begin
  if v_user_id is null then raise exception 'Authentication required.'; end if;
  return jsonb_build_object('Orders', coalesce((
    select jsonb_agg(jsonb_build_object(
      'OrderId', o.id::text, 'SellerAccountId', o.seller_id::text, 'ItemId', o.item_id,
      'RemainingQuantity', o.remaining_quantity, 'UnitPrice', o.unit_price, 'Status', o.status
    ) order by o.unit_price, o.created_at)
    from public.market_orders o where o.status = 'active'
  ), '[]'::jsonb));
end;
$$;

create or replace function public.farm_create_market_order(p_request jsonb)
returns jsonb language plpgsql security definer set search_path = public, pg_temp as $$
declare
  v_user_id uuid := auth.uid();
  v_request_id uuid;
  v_item_id text;
  v_quantity integer;
  v_unit_price integer;
  v_owned integer;
  v_order public.market_orders%rowtype;
  v_cached jsonb;
begin
  if v_user_id is null then raise exception 'Authentication required.'; end if;
  v_request_id := nullif(p_request->>'RequestId', '')::uuid;
  v_item_id := nullif(trim(p_request->>'ItemId'), '');
  v_quantity := (p_request->>'Quantity')::integer;
  v_unit_price := (p_request->>'UnitPrice')::integer;
  if v_request_id is null or v_item_id is null or v_quantity < 1 or v_unit_price < 1 then raise exception 'Invalid market listing request.'; end if;
  perform public.farm_ensure_account(v_user_id);

  select response into v_cached from public.market_command_receipts where user_id = v_user_id and request_id = v_request_id and operation = 'market.create';
  if v_cached is not null then return v_cached; end if;
  if exists (select 1 from public.market_command_receipts where user_id = v_user_id and request_id = v_request_id) then raise exception 'Request ID was already used for a different operation.'; end if;

  select quantity into v_owned from public.farm_inventory where user_id = v_user_id and item_id = v_item_id for update;
  if coalesce(v_owned, 0) < v_quantity then raise exception 'Insufficient inventory for this listing.'; end if;
  update public.farm_inventory set quantity = quantity - v_quantity, revision = revision + 1, updated_at = now() where user_id = v_user_id and item_id = v_item_id;
  insert into public.market_orders (seller_id, item_id, original_quantity, remaining_quantity, unit_price)
    values (v_user_id, v_item_id, v_quantity, v_quantity, v_unit_price) returning * into v_order;
  v_cached := jsonb_build_object('Succeeded', true, 'Message', 'Market listing created.', 'Order', jsonb_build_object(
    'OrderId', v_order.id::text, 'SellerAccountId', v_order.seller_id::text, 'ItemId', v_order.item_id,
    'RemainingQuantity', v_order.remaining_quantity, 'UnitPrice', v_order.unit_price, 'Status', v_order.status));
  insert into public.market_command_receipts (user_id, request_id, operation, response) values (v_user_id, v_request_id, 'market.create', v_cached);
  return v_cached;
end;
$$;

create or replace function public.farm_buy_market_order(p_request jsonb)
returns jsonb language plpgsql security definer set search_path = public, pg_temp as $$
declare
  v_user_id uuid := auth.uid();
  v_request_id uuid;
  v_order_id uuid;
  v_quantity integer;
  v_cost bigint;
  v_order public.market_orders%rowtype;
  v_buyer_wallet integer;
  v_cached jsonb;
begin
  if v_user_id is null then raise exception 'Authentication required.'; end if;
  v_request_id := nullif(p_request->>'RequestId', '')::uuid;
  v_order_id := nullif(p_request->>'OrderId', '')::uuid;
  v_quantity := (p_request->>'Quantity')::integer;
  if v_request_id is null or v_order_id is null or v_quantity < 1 then raise exception 'Invalid market purchase request.'; end if;
  perform public.farm_ensure_account(v_user_id);
  select response into v_cached from public.market_command_receipts where user_id = v_user_id and request_id = v_request_id and operation = 'market.buy';
  if v_cached is not null then return v_cached; end if;
  if exists (select 1 from public.market_command_receipts where user_id = v_user_id and request_id = v_request_id) then raise exception 'Request ID was already used for a different operation.'; end if;

  select * into v_order from public.market_orders where id = v_order_id for update;
  if not found or v_order.status <> 'active' or v_order.remaining_quantity < v_quantity then raise exception 'This market order is no longer available.'; end if;
  if v_order.seller_id = v_user_id then raise exception 'You cannot buy your own listing.'; end if;
  v_cost := v_order.unit_price::bigint * v_quantity::bigint;
  if v_cost > 2147483647 then raise exception 'Market purchase exceeds the supported wallet range.'; end if;
  perform 1 from public.farm_accounts where user_id in (v_user_id, v_order.seller_id) order by user_id for update;
  select wallet into v_buyer_wallet from public.farm_accounts where user_id = v_user_id;
  if v_buyer_wallet < v_cost then raise exception 'Insufficient funds.'; end if;
  update public.farm_accounts set wallet = wallet - v_cost::integer, revision = revision + 1, updated_at = now() where user_id = v_user_id;
  update public.farm_accounts set wallet = wallet + v_cost::integer, revision = revision + 1, updated_at = now() where user_id = v_order.seller_id;
  insert into public.farm_inventory (user_id, item_id, quantity) values (v_user_id, v_order.item_id, v_quantity)
    on conflict (user_id, item_id) do update set quantity = public.farm_inventory.quantity + excluded.quantity, revision = public.farm_inventory.revision + 1, updated_at = now();
  update public.market_orders set remaining_quantity = remaining_quantity - v_quantity,
    status = case when remaining_quantity - v_quantity = 0 then 'fulfilled' else 'active' end, updated_at = now()
    where id = v_order.id returning * into v_order;
  v_cached := jsonb_build_object('Succeeded', true, 'Message', 'Market purchase confirmed.', 'Order', jsonb_build_object(
    'OrderId', v_order.id::text, 'SellerAccountId', v_order.seller_id::text, 'ItemId', v_order.item_id,
    'RemainingQuantity', v_order.remaining_quantity, 'UnitPrice', v_order.unit_price, 'Status', v_order.status),
    'Account', (select jsonb_build_object('AccountId', a.user_id::text, 'Revision', a.revision, 'Wallet', a.wallet, 'Inventory', '[]'::jsonb) from public.farm_accounts a where a.user_id = v_user_id));
  insert into public.market_command_receipts (user_id, request_id, operation, response) values (v_user_id, v_request_id, 'market.buy', v_cached);
  return v_cached;
end;
$$;

revoke all on function public.farm_get_account_snapshot() from public;
revoke all on function public.farm_list_active_market_orders() from public;
revoke all on function public.farm_create_market_order(jsonb) from public;
revoke all on function public.farm_buy_market_order(jsonb) from public;
revoke all on function public.farm_ensure_account(uuid) from public;
grant execute on function public.farm_get_account_snapshot(), public.farm_list_active_market_orders(), public.farm_create_market_order(jsonb), public.farm_buy_market_order(jsonb) to authenticated;
