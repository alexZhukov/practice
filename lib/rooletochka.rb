# encoding: utf-8

require 'sequel'
require 'pg'

module Rooletochka
end
DBNAME = 'practice'
USERNAME = 's'
DB = Sequel.postgres(database: DBNAME, user: USERNAME, host: 'localhost')

require 'sinatra'
require 'sinatra-authentication'
require 'haml'
require 'zurb-foundation'

require 'practice/frontend'