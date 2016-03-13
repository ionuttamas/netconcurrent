This project is copied from Spring.Threading project code, which was originally a port of Java's concurrent API. The reason of copying is because I need to fix some issue and add something more but cannot wait for the original project to complete. There is no intention to have it in anyway to compete or replace the original project. It will likely be a temp home and the work will, hopefully, be merged back to its origin.

Update: I had since taken over the primary responsibility of the original Spring.Threading project http://www.springsource.org/extensions/se-threading-net. Code was then integrated there.

As the project evolves, a lot of .Net 4.0 Parallel Task Library features are added to the project making the project a great alternative to PTL, especially in the .Net 2.0.

You can download the binary with API doc in the download area here. The current beta binary has been used in a production environment for 2.5 years for features listed below:

  * Thread pool and executor service
  * Atomic classes
  * Analogue function of Parallel.ForEach for executor services.
  * Analogue function of Task.WaitAll and Task.WaitAny for executor services.