# apsystems_check_for_dead_inverters

What is this app? Well I have 1-2 months where I don't look at my 
cool solar panel's performance and TWICE now I have had 2 inverters crap out
so why not once a day - run this for "yesterday" and see the next day when 
an inverter goes. I did pay for the extra warantee - so lets use it. Time is
money ... literally.

What it does
- gets the daily kwH for all inverters "yesterday"
- calcs its percentage of the total
- if percentage is < .2% for any inverter, then it sends an email with the offending inverter id's



This is my first ap systems app - MANY THANKS to my german friends at this website 
https://gathering.tweakers.net/forum/list_messages/2032302/10


Note the AP Systems API guide is here
https://file.apsystemsema.com:8083/apsystems/resource/openapi/Apsystems_OpenAPI_User_Manual_End_User_EN.pdf
- its likley java - so with the help of my german friends and their bash equiv code
 I learned what the heck the API docs wanted and then converted it to c#

What tripped me up most
a) timestamp - it is a unix integer! Being a database guy - its not yyyy-mm-dd hh:mm::system
b) the root url - took me like 1.25 hours of googling to get that. Its in system.json
    - thats what led me to our german buddies above
c) there is a phrase in the guide "RequestPath (The last name of the path)" - it means if you have a url
    like /this/is/part/of/a/url then "url" is all they want.

Other than that this is a piece of cake. Yes the guide needs a revision but its good other than that

Steps to start rocking out
a) COPY mail.sample.json to mail.json
    - edit the mail.json with your info
    - it does NOT allow for Oauth mail yet
b) COPY rename system.sample.json to system.json
    - edit system.json
    - log into ap systems and get all the settings into it
    - enable the api unlock the api secret
    - use it more than 1x every 6 months
    - https://apsystemsema.com/ema/index.action

DO NOT PUT mail.json and system.json into git - there is a rule that disallows this

