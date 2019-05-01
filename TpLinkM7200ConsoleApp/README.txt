How to use
----------------
To Control a TP Link M7200 from a desktop Application

Usage: TpLinkM7200ConsoleApp [options] [command]

Options:
  -?|-h|--help              Show help information
  -url <URL>                Router URL address
  -p|--Password <PASSWORD>  Router Password

Commands:
  Restart                   Restart the router

Run 'TpLinkM7200ConsoleApp [command] --help' for more information about a command.


How to compile
----------------
 dotnet publish -c Release -r ubuntu.16.04-x64
 dotnet publish -c Release -r win10-x64