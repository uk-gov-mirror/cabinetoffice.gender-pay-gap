# LoadTests

We use [Gatling](https://gatling.io/) for the purpose of Load Testing. Gatling is an open-source load and performance testing framework based on Scala, Akka and Netty.

## Zero to Hero

This module can't be opened in Visual Studio so you'll need another IDE such as IntelliJ.

1. Download and install [IntelliJ](https://www.jetbrains.com/idea/download) (Community edition is fine)
1. Install **Java** 64bits JDK 8 (not compatible with JDK 12+)
1. Set up your JDK if you didn't already - you'll need to see a valid Java 1.8 under File > Project Structure > Project SDKs
1. Install **Scala** 2.12 (not compatible with Scala 2.11 or Scala 2.13)
1. Install the Scala plugin for IntelliJ: File > Settings > Plugins > search for Scala and hit Install
1. Increase the stack size for the Scala compiler so you don't suffer from StackOverflowErrors. Recommended setting Xss to 100M.  (File -> Settings -> Build, Execution, Deployment -> Compiler -> Scala Compiler -> Scala Compile Server -> in the JVM options textbox)
1. Open just this folder (./LoadTests) in IntelliJ
1. Maven should automatically sync, but if it doesn't, open the Maven window and hit "Reimport all Maven projects" (the "refresh" icon)

You should now be able to run
```
mvn gatling:verify
```
and have it succeed.

### Creating or modifying a scenario

The scenarios in this project were originally created using the Gatling Recorder. For minor tweaks (e.g. just to a specific page) you can edit `RecordingSimulation.scala` directly, or for a more substantial change you should consider recording a new scenario from scratch. See the [Gatling recorder docs](https://gatling.io/docs/current/quickstart/#using-the-recorder) for more.

## Running the load test

The GPG service should be load tested once a year, a few months in advance of the main reporting period in late March. You'll need to
1. Create a **new environment** to run the load test against
1. Set up the **database** with appropriate test data
1. **Calibrate** the test with an appropriate number of users
1. **Run** the actual test
1. **Analyse** the results

### 1. Creating a new environment

Don't run load tests against Live. Ideally, create a completely fresh environment to run your load tests against and collapse it when you're done.

* Bring the `load-test` branch up to date with master.
* Use the `Infrastructure/CreateEnvironment.sh` script to set up a new environment.
* Set it to use `Web.loadtest.Config`
* Be sure to set the instance sizes etc. to mirror Live.
* However, you can skip all the stuff about system monitoring, logit etc - as the environment will only be needed for the duration of the load test.
* Set the `TESTING-SkipSpamProtection` config var to True

### 2. Setting up the database

There's a script for this, `SET_UP_TEST_ENTITIES.sql`. Run it against your new database.

There are scripts `ConnectToPaas_DB_{env}.sh` in the Infrastructure folder - you'll need to tweak them as required if your environment has a different name.

### 3. Calibrate the test

Some things you might want to calibrate: the number of users; the rate at which they arrive; the total time for the test (currently 5 hours).

The current version of the sql script and the tests are written for 1000 test users. If you want to change this number to <x>:

* Change the initial value of `@NUM_OF_USERS` in the sql script to be <x>
* Change the initial value of `MAX_NUM_USERS` in RecordingSimulation to be <x>
* Change the `users_organisations.csv` to include
  * user1@example.com through user<x>@example.com, AND
  * test_<x+1> to test_<2x>, AND
  * 99999<x+1> to 99999<2x>

### 4. Run the actual test

* From the command line:
```
mvn gatling:test
```

* or, in IntelliJ, Maven > LoadTests > Plugins > gatling > gatling:test (see the [Gatling Maven plugin docs](https://gatling.io/docs/current/extensions/maven_plugin))
 * or right click on `Engine` and run it.

### 5. Analyse the results

You can find the results report in `LoadTests/target/gatling`.

The results of past load tests, along with some back-of-the-envelope calculations of expected peak usage, are on the wiki. Ask a member of the team for access.