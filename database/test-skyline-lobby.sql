begin;
select set_config('skyline.test_host',gen_random_uuid()::text,true);
select set_config('skyline.test_guest',gen_random_uuid()::text,true);
select set_config('skyline.test_stranger',gen_random_uuid()::text,true);
insert into auth.users(id,email) select id,id::text || '@example.invalid' from (values(current_setting('skyline.test_host')::uuid),(current_setting('skyline.test_guest')::uuid),(current_setting('skyline.test_stranger')::uuid)) as u(id);
set local role authenticated;
select set_config('request.jwt.claim.sub',current_setting('skyline.test_host'),true);
select set_config('skyline.test_room',(public.skyline_create_room('duel','heights','127.0.0.1',29801,'TestOnly93')->>'room_id'),true);
select set_config('skyline.test_code',(public.skyline_lobby_action(current_setting('skyline.test_room')::uuid)->>'room_code'),true);
select set_config('request.jwt.claim.sub',current_setting('skyline.test_guest'),true);
select public.skyline_join_room(current_setting('skyline.test_code'));
do $test$
declare r jsonb;
begin
 r:=public.skyline_lobby_action(current_setting('skyline.test_room')::uuid,'ready',true);
 if jsonb_array_length(r->'players')<>2 then raise exception 'Roster incomplete'; end if;
 if exists(select 1 from jsonb_array_elements(r->'players') p where (p->>'ready')::boolean=false) then raise exception 'Ready failed'; end if;
 begin
   perform public.skyline_lobby_action(current_setting('skyline.test_room')::uuid,'close');
   raise exception 'Guest could close host room';
 exception when insufficient_privilege then null; end;
end $test$;
select set_config('request.jwt.claim.sub',current_setting('skyline.test_stranger'),true);
do $test$ begin
 begin
   perform public.skyline_lobby_action(current_setting('skyline.test_room')::uuid);
   raise exception 'Nonmember could read roster';
 exception when insufficient_privilege then null; end;
 begin
   perform public.skyline_join_room(current_setting('skyline.test_code'));
   raise exception 'Full room admitted third player';
 exception when raise_exception then if SQLERRM<>'Room is full' then raise; end if; end;
end $test$;
select set_config('request.jwt.claim.sub',current_setting('skyline.test_host'),true);
select public.skyline_lobby_action(current_setting('skyline.test_room')::uuid,'leave');
reset role;
do $test$ begin
 if exists(select 1 from public.skyline_room_members where room_id=current_setting('skyline.test_room')::uuid) then raise exception 'Host leave did not clean membership'; end if;
 if (select status from public.skyline_rooms where id=current_setting('skyline.test_room')::uuid)<>'closed' then raise exception 'Host leave did not close room'; end if;
end $test$;
rollback;
select 'PASS: create, join, roster, ready, full room, nonmember denial, guest controls denial, host leave cleanup; test data rolled back' as result;
