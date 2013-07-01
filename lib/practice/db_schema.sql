CREATE TABLE users (
  id serial primary key,
  name varchar(40) not null,
  password varchar(20) not null
);

CREATE TABLE sites (
  id serial primary key,
  url char(250) unique,
  ready boolean default 'false'
);


CREATE TABLE reports (
  id serial primary key,
  site_id integer references sites(id) on update cascade,
  pages text[],
  rules hstore[],
  path char(250)
);

CREATE TABLE rules (
  id integer primary key,
  name varchar(120) not null,
  common boolean default 'true',
  message varchar(1500) not null
);

CREATE TABLE user_site (
  user_id integer references users(id) on update cascade,
  site_id integer references sites(id) on update cascade,
  unique (user_id, site_id)
);
