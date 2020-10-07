# hetrodo.Utilities.MainThread

This is a wrapper class for executing heavy calculations without freezing the main thread but still
using [Unity's api]("https://docs.unity3d.com/ScriptReference/") (like transform.position, Instantiate, Destroy, etc).



# Reference

### MainThread


*   ```cs
        Exec(System.Action action) //Method Synchronous
    ``` 
    Executes an action on the main thread, and freezes its caller thread until execution.
 

*   ```cs
        ExecAsync(System.Action action) //Method Asynchronous
    ``` 
    Executes an action on the main thread without freezing.


*   ```cs
        IsRunning //Boolean Field
    ``` 
    Tells if you had already initialized the MainThread class.


*   ```cs
        OnExceptionCaught(Exception ex) //Error Event
    ``` 
    If any exception occurs while executing the actions, this event will be fired.


### MainThread.Timing


*   ```cs
        DeltaTime //Float Field
    ``` 
    Time since last call for each thread.



# Tips


Try to minimize MainThread.Exec usage, as every call will add at least 25ms to your code. If you
are using the MainThread.ExecAsync in a loop be sure to add a Thread.Sleep to not overflow the
execution pool.