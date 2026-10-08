do $$
declare
  routine regprocedure;
begin
  if to_regclass('public.caravans') is null then
    raise notice 'Le jeu de commerce est déjà effacé.';
    return;
  end if;

  for routine in
    select p.oid::regprocedure
    from pg_proc p
    where p.pronamespace = 'public'::regnamespace
      and p.proname = any (array[
        'accept_contract', 'accept_offer', 'answer_event', 'attach_wagon', 'buy_goods', 'buy_wagon',
        'cancel_contract', 'cancel_offer', 'choose_talent', 'collect_odd_jobs', 'decline_special_order',
        'depart', 'deposit_goods', 'found_player', 'fulfill_special_order', 'get_state', 'get_world',
        'install_fitting', 'mark_exchanges_seen', 'mark_journal_seen', 'open_branch', 'post_contract',
        'post_offer', 'sell_goods', 'sell_masterpiece', 'set_standing_order', 'start_production',
        'upgrade_workshop', 'withdraw_goods'
      ])
  loop
    execute format('drop function %s cascade', routine);
  end loop;

  drop table if exists
    public.branches, public.caravan_fittings, public.caravans, public.cargo, public.crafts,
    public.directive_choices, public.directives, public.event_choices, public.event_kinds,
    public.event_log, public.fitting_costs, public.fittings, public.goods, public.markets,
    public.masteries, public.masterpieces, public.offers, public.pending_events, public.player_talents,
    public.players, public.recipe_inputs, public.recipes, public.reputation_tiers, public.reputations,
    public.road_event_odds, public.routes, public.special_orders, public.standing_orders, public.talents,
    public.towns, public.trade_log, public.transport_contracts, public.trip_events, public.warehouses,
    public.workshops, public.saves
  cascade;

  drop schema if exists private cascade;
  raise notice 'Le jeu de commerce est effacé. Les comptes sont gardés.';
end;
$$;
