# elmah.io.log4net
Log to elmah.io from log4net.

## Installation
elmah.io.log4net installs through NuGet:

```
PS> Install-Package elmah.io.log4net
```

Add the elmah.io appender to your log4net.config:

```xml
<root>
  <appender-ref ref="ElmahIoAppender" />
</root>

<appender name="ElmahIoAppender" type="Elmah.Io.Log4Net.ElmahIoAppender, elmah.io.log4net">
  <logId value="ELMAH_IO_LOG_ID" />
  <filter type="log4net.Filter.LevelRangeFilter">
    <param name="LevelMin" value="INFO"/>
  </filter>
</appender>
```

In the example we specify the level minimum as INFO. This tells log4net to log only information, warnings, errors and fatals in log4net. You may adjust this but be aware, that your elmah.io log may run full pretty fast, if you log thousands and thousands of verbose messages.

## Usage
Log messages to elmah.io, just as with every other appender and log4net:

```c#
log.Info("This is an information message");
log.Error("This is an error message", ex);
```
