CREATE TABLE sequel_users (
  id serial primary key,
  email varchar(40) unique,
  hashed_password varchar(80) not null,
  salt varchar(40),
  created_at timestamp,
  permission_level integer default 1
);

CREATE TYPE state as enum ('nothing', 'data', 'report', 'processing', 'failed');

CREATE TABLE sites (
  id serial primary key,
  url varchar(250) unique,
  ready state default 'nothing'
);


CREATE TABLE reports (
  id serial primary key,
  site_id integer references sites(id) on update cascade,
  creation_time timestamp,
  path varchar(250)
);

CREATE TABLE subpages (
  id serial primary key,
  url varchar(250),
  rules json,
  report_id integer references reports(id)
);

CREATE TABLE rules (
  id serial primary key,
  name varchar(120) not null,
  common boolean default 'true',
  message varchar(1500) not null
);

CREATE TABLE user_site (
  user_id integer references sequel_users(id) on update cascade,
  site_id integer references sites(id) on update cascade,
  unique (user_id, site_id)
);