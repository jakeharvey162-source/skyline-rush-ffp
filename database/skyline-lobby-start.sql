-- Add an explicit host-only Start Match transition to the existing Skyline lobby.
-- Keeps the current tables/RLS and public RPC signature intact.

create or replace function skyline_private.lobby_action(
  p_room_id uuid,
  p_action text default 'view',
  p_ready boolean default null
)
returns jsonb
language plpgsql
security definer
set search_path = pg_catalog
as $$
declare
 uid uuid := auth.uid();
 room public.skyline_rooms;
 players jsonb;
 member_count integer;
 ready_count integer;
begin
 if uid is null then
   raise exception 'Sign in to use multiplayer' using errcode='42501';
 end if;

 if p_action is null or p_action not in ('view','ready','leave','close','start') then
   raise exception 'Unsupported lobby action';
 end if;

 select * into room
 from public.skyline_rooms
 where id=p_room_id
 for update;

 if not found or not exists(
   select 1 from public.skyline_room_members
   where room_id=p_room_id and user_id=uid
 ) then
   raise exception 'Room unavailable or you are not a member' using errcode='42501';
 end if;

 if p_action='leave' then
   if uid=room.host_user_id then
     update public.skyline_rooms set status='closed' where id=p_room_id;
     delete from public.skyline_room_members where room_id=p_room_id;
   else
     delete from public.skyline_room_members where room_id=p_room_id and user_id=uid;
   end if;
   return jsonb_build_object('left',true,'host_left',uid=room.host_user_id);
 end if;

 if p_action='close' then
   if uid<>room.host_user_id then
     raise exception 'Only the host can close this room' using errcode='42501';
   end if;
   update public.skyline_rooms set status='closed' where id=p_room_id;
   delete from public.skyline_room_members where room_id=p_room_id;
   return jsonb_build_object('closed',true);
 end if;

 if room.status='closed' or room.expires_at<=now() then
   raise exception 'Room closed or expired';
 end if;

 if p_action='ready' then
   if p_ready is null then raise exception 'Choose ready or not ready'; end if;
   if room.status<>'open' then raise exception 'Match is already starting or in progress'; end if;
   update public.skyline_room_members
     set ready=p_ready
   where room_id=p_room_id and user_id=uid;
 end if;

 if p_action='start' then
   if uid<>room.host_user_id then
     raise exception 'Only the host can start this match' using errcode='42501';
   end if;
   if room.status<>'open' then
     raise exception 'Match has already started';
   end if;

   select count(*), count(*) filter (where ready)
     into member_count, ready_count
   from public.skyline_room_members
   where room_id=p_room_id;

   if member_count < 1 then raise exception 'Room has no players'; end if;
   if ready_count <> member_count then
     raise exception 'Every player must be ready before starting';
   end if;

   update public.skyline_rooms set status='starting' where id=p_room_id;
   room.status := 'starting';
 end if;

 select coalesce(jsonb_agg(jsonb_build_object(
   'user_id',m.user_id,
   'display_name',coalesce(p.display_name,'Player'),
   'team',m.team,
   'ready',m.ready,
   'is_host',m.user_id=room.host_user_id
 ) order by m.joined_at,m.user_id),'[]'::jsonb)
 into players
 from public.skyline_room_members m
 left join public.skyline_profiles p using(user_id)
 where m.room_id=p_room_id;

 return jsonb_build_object(
   'room_id',room.id,
   'room_code',room.room_code,
   'mode',room.mode,
   'district',room.district,
   'status',room.status,
   'max_players',room.max_players,
   'expires_at',room.expires_at,
   'players',players
 );
end
$$;
