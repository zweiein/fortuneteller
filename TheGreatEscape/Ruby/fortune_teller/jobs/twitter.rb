require 'twitter'


#### Get your twitter keys & secrets:
#### https://dev.twitter.com/docs/auth/tokens-devtwittercom
twitter = Twitter::REST::Client.new do |config|
  config.consumer_key = 'BZGaBFIVuIpgLMa56HTqqy5WB'
  config.consumer_secret = 'anUOoyzenNompwYCKlZEm9rUyM6c9Z4rU1xNTYxCzYEjzBfjrb'
  config.access_token = '102642438-PdfZ34UAC0MmHwb0NOAVTGIw8cjvPaode7XAuSui'
  config.access_token_secret = 'vMHfmGcNe3CrJf7dD3BWQVSPzfz3D9IaphPWB5LYHTPYi'
end

search_term = URI::encode('#theapprentice')

SCHEDULER.every '10m', :first_in => 0 do |job|
  begin
    tweets = twitter.search("#{search_term}")

    if tweets
      tweets = tweets.map do |tweet|
        { name: tweet.user.name, body: tweet.text, avatar: tweet.user.profile_image_url_https }
      end
      send_event('twitter_mentions', comments: tweets)
    end
  rescue Twitter::Error
    puts "\e[33mFor the twitter widget to work, you need to put in your twitter API keys in the jobs/twitter.rb file.\e[0m"
  end
end