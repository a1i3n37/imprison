# Imprison

Quick and dirty project for comfortably setting up a single-app kiosk user on windows.

## Usage

In an elevated console (with administrative rights)

    Imprison.exe UserName /pw:password AppPath

* username - the username you want to 'imprison'
* password (optional) - the password to the given username
* AppPath - the single application you want to limit the user to

If the specified user does not exist, it will be created and added to the Users group automatically.

## How it works

* Imprison replaces the system shell (explorer.exe) with the application you specified for the given user. 
* Additionally, the task manager and registry editor are disabled for the given user.
* Automatic login will be set to login the kiosk user.

You can read more about shell replacement [here](https://docs.microsoft.com/en-us/previous-versions/windows/embedded/ms838576(v=winembedded.5))

## License

This project is licensed unter MIT License - see [LICENSE.md](LICENSE.md) file for details

## Acknowledgements

* [Impersonation Demo](https://www.codeproject.com/Articles/124981/Impersonating-user-accessing-files-and-HKCU)
