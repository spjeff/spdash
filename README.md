## Description
Easily compare server configuration. "Grid" layout shows configuration of the full farm in one table. Export to Excel for filter, sort, and pivot chart.

[![](https://raw.githubusercontent.com/spjeff/spdash/master/doc/download.png)](https://github.com/spjeff/spdash/releases/download/SPDash/SPDash.wsp)

## Benefits
* Do admin tasks faster
* Scale up beyond RDP
* Add new servers without fear
* Verify consistent configuration

## Features
* New WSP format
* Central Admin > Monitoring > SPDash easy navigation
* View server config data in table format
* Export to Excel
* Timer job refreshes data cache of XML files
* Wide screen layout for SharePoint 2013
* Automatic hive (14/15/16) detection for SP2010/2013/2016 support

## Data Sources
* WMI query
* Global Assembly Cache
* IIS web.config
* Logical disk size and space
* File versions (to confirm patching)
* Display Local Administrator members
* Kerberos SPN for all managed accounts and farm machines

## Background
SharePoint farms have many servers with various services, applications, traffic patterns, and purposes. Having "grid" scripts like the below sample screen-shots can save lots of time. What's a "grid"? Simple. I wanted to build a real-time Excel spreadsheet to display ALL configuration without RDP. RDP won't scale. While great for 1-2 servers, it's awful for 10+ servers. Having confidence in your configs and knowing everything is 100% consistent are BIG steps forward for most admins.

## Screenshots
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/1.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/2.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/3.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/4.png)

![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/5.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/6.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/7.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/8.png)

![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/9.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/10.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/11.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/12.png)

![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/13.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/14.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/15.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/16.png)

![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/17.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/18.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/19.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/20.png)

![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/21.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/22.png)
![image](https://raw.githubusercontent.com/spjeff/spdash/master/doc/23.png)

## Contact
Please drop a line to [@spjeff](https://twitter.com/spjeff) or [spjeff@spjeff.com](mailto:spjeff@spjeff.com)
Thanks!  =)

![image](http://img.shields.io/badge/first--timers--only-friendly-blue.svg?style=flat-square)

## License

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.