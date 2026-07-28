-- Meta 8 foundation. Apply through the Supabase CLI after a real project exists.
-- The Unity build never receives a service_role/secret key and never writes these
-- tables directly. Mutations are performed only through authenticated functions/RPCs.

create table if not exists public.farm_accounts (
  user_id uuid primary key references auth.users(id) on delete cascade,
  wallet integer not null default 0 check (wallet >= 0),
  revision bigint not null default 1 check (revision > 0),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create table if not exists public.farm_inventory (
  user_id uuid not null references public.farm_accounts(user_id) on delete cascade,
  item_id text not null,
  quantity integer not null check (quantity >= 0),
  revision bigint not null default 1 check (revision > 0),
  updated_at timestamptz not null default now(),
  primary key (user_id, item_id)
);

create table if not exists public.market_orders (
  id uuid primary key default gen_random_uuid(),
  seller_id uuid not null references public.farm_accounts(user_id),
  item_id text not null,
  original_quantity integer not null check (original_quantity > 0),
  remaining_quantity integer not null check (remaining_quantity >= 0 and remaining_quantity <= original_quantity),
  unit_price integer not null check (unit_price > 0),
  status text not null default 'active' check (status in ('active', 'fulfilled', 'cancelled')),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create table if not exists public.market_command_receipts (
  user_id uuid not null references public.farm_accounts(user_id) on delete cascade,
  request_id uuid not null,
  operation text not null check (operation in ('market.create', 'market.buy', 'market.cancel')),
  response jsonb not null,
  created_at timestamptz not null default now(),
  primary key (user_id, request_id)
);

alter table public.farm_accounts enable row level security;
alter table public.farm_inventory enable row level security;
alter table public.market_orders enable row level security;
alter table public.market_command_receipts enable row level security;

-- Read-only personal account access. Do not create client INSERT/UPDATE/DELETE policies.
create policy "account owner reads own account" on public.farm_accounts for select to authenticated using (auth.uid() = user_id);
create policy "inventory owner reads own inventory" on public.farm_inventory for select to authenticated using (auth.uid() = user_id);
create policy "all signed-in players read active market orders" on public.market_orders for select to authenticated using (status = 'active' or seller_id = auth.uid());
create policy "receipt owner reads own receipts" on public.market_command_receipts for select to authenticated using (auth.uid() = user_id);

-- The next migration must define transactional SECURITY DEFINER RPCs for create,
-- buy, and cancel. Each RPC locks the market row and buyer/seller accounts with
-- FOR UPDATE, records the request_id receipt, and returns the stored receipt on
-- a retry. Keep their schema-qualified search_path fixed and grant execute only
-- to authenticated. Do not put money or inventory mutation in a client policy.
