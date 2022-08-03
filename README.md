# ResourceHealthChecker
Provides ability of a service, api or app to check on the health of its required resources periodically

## How it works

The application defines one or more Health Checks, such as a file system check or a SQL Server check.  More than one check of the same type can be setup.

Upon initial start it will wait a certain amount of time to ensure all checks are successful.  If they are not then it will throw and the application will shut down.

While running if it detects a change in the health of one or more health checks it will log the change at that time.  It will not log any further errors until it changes again.  That way you are not inundated with bad health checks.

The Resource Health Checker sets up a background thread that runs periodically and then sleeps.  


