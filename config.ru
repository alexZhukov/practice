#!/usr/bin/env rackup
# encoding: utf-8

$:.unshift File.expand_path('../lib', __FILE__)

require 'bundler/setup'
require 'rooletochka'

run Rooletochka::Frontend
