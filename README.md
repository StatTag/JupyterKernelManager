# JupyterKernelManager

A C# library that allows you to run locally installed Jupyter kernels and interact with them to execute code and return results.

_This is currently in a pre-release state.  It is able to demonstrate proof of concept, but does not include all necessary features or error checking._

## Getting Started

Clone the repository, and open `JupyterKernelManager/JupyterKernelManager.sln`.  This will load the library project (`JupyterKernelManager`), the unit tests (`Tests`), as well as a simple command line test program (`LocalTest`).


### Prerequisites

This project has only been tested with .NET 4.5.1 on the Windows operating system.  It assumes you have Visual Studio 2017 or higher (this will work with Community Edition)

### Installing

Open the solution in Visual Studio where you want to incorporate Jupyter kernel functionality.  See [StatTag](https://github.com/StatTag/StatTag) for an example project.

Right click on your solution, and select `Add -> Existing project...`.  Navigate to `JupyterKernelManager/JupyterKernelManager` and add `JupyterKernelManager.csproj` to the solution.  You can now add JupyterKernelManager as a dependency to your solution.


### Using the Library

#### Listing the installed kernels

The `KernelSpecManager` class will handle finding and enumerating the installed kernels on your system.  The following code snippet will retrieve a list of `KernelSpec` objects that represent the kernels you have installed.

```
var manager = new KernelSpecManager();
var kernelSpecs = manager.GetAllSpecs();
Console.WriteLine("Enumerating all kernels");
foreach (var kernelSpec in kernelSpecs)
{
	Console.WriteLine("   Found {0} at {1}", kernelSpec.Key, kernelSpec.Value.ResourceDirectory);
}
```

If you have a kernel installed, but it is not showing up in the list returned from `GetAllSpecs`, you may need to explicitly register your kernel in the user directory.  From the command line on your system, run the following command (the example is for the MATLAB kernel):

```
python -m matlab_kernel install --user
```

#### Connecting to a kernel
To interact with the kernel, you can circumvent the `KernelSpecManager`, and use the `KernelManager` class directly.  The `KernelManager` class handles all of the identification and launching of one specific kernel.  Once you have created a `KernelManager` object, you can use it to create a `KernelClient`.  This object allows you to send and receive messages from the kernel.  The following example shows basic initialization of a `KernelClient` for [IRKernel](https://github.com/IRkernel/IRkernel).

```
using (var kernelManager = new KernelManager("ir"))
{
	kernelManager.StartKernel();
	var client = kernelManager.CreateClient();
	client.StartChannels();

	// Send a simple R command to be run
	client.Execute("x <- 100; x");

	// Wait until all of our code has been executed
	while (client.HasPendingExecute())
	{
		Thread.Sleep(500);
	}

	client.StopChannels();

	// Now echo out everything returned by the kernel
	var executeLog = client.ExecuteLog.Values.OrderBy(x => x.ExecutionIndex);
	foreach (var entry in executeLog)
	{
		Console.WriteLine("Item {0} ------------------------------------------", entry.ExecutionIndex);
		Console.WriteLine(entry.Request.Content.code);
		Console.WriteLine();

		var dataResponse = entry.Response.FirstOrDefault(
			x => x.Header.MessageType.Equals(MessageType.DisplayData) || x.Header.MessageType.Equals(MessageType.Stream));
		if (dataResponse == null)
		{
			Console.WriteLine("  ( No data returned for this code block )");
		}
		else
		{
			Console.WriteLine(dataResponse.Content);
		}
		Console.WriteLine("--------------------------------------------------\r\n");
	}
}
```

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Acknowledgments

Many thanks to the following wonderful projects for providing code that was ported directly, or reviewed and used to inspire/inform the creation of this library.

* [Jupyter's Python client](https://github.com/jupyter/jupyter_client)
* [Microsoft's C# Jupyter kernel library](https://github.com/microsoft/jupyter-core)
* [Nteract's JavaScript Hydrogen environment](https://github.com/nteract/hydrogen)


_This work was developed within the Northwestern University Clinical and Translational Sciences Institute, supported in part by the National Institutes of Health’s National Center for Advancing Translational Sciences (grant UL1TR001422).  The content is solely the responsibility of the developers and does not necessarily represent the official views of the National Institutes of Health or Northwestern University._
