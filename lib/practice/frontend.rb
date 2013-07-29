#encoding: utf-8

# Sinatra application.
#
class Rooletochka::Frontend < Sinatra::Base
  use Rack::Session::Cookie, secret: '9l frontend Mpa3u'

  set app_file: __FILE__
  set views: File.expand_path('../../../views', app_file)
  set public_folder: File.expand_path('../../../public', app_file)

  register Sinatra::SinatraAuthentication

  set :haml, :format => :html5

  get '/' do
    p @reports = reports(current_user.values[:id])
    haml :reports
  end

  post '/add_site' do
    add_site(current_user.values[:id], params[:uri])
    haml :done
  end

  protected

  # Get sites ids given user id.
  #
  def sites_ids(user_id)
    DB[:user_site].where(user_id: user_id).map(:site_id)
  end

  # Get reports given user id.
  #
  def reports(user_id)
    sites = sites_ids(user_id)
    DB[:reports].select(:sites__url, :sites__ready, :creation_time, :path).
      inner_join(:sites, :id => :site_id).where(site_id: sites).all
  end

  # Add site and link it with user.
  #
  def add_site(user_id, url)
    site_id = DB[:sites].insert(url: url)
    DB[:user_site].insert(user_id: user_id, site_id: site_id)
  end
end