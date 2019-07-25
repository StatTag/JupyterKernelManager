# Contributing to JupyterKernelManager

:+1::tada: Thanks for taking the time to contribute! :tada::+1:

The following is a set of guidelines for contributing to StatTag and its packages, which are hosted in the [StatTag Organization](https://github.com/StatTag) on GitHub. These are mostly guidelines, not rules. Use your best judgment, and feel free to propose changes to this document in a pull request.

#### Table Of Contents

[Code of Conduct](#code-of-conduct)

[I don't want to read this whole thing, I just have a question!!!](#i-dont-want-to-read-this-whole-thing-i-just-have-a-question)

[What should I know before I get started?](#what-should-i-know-before-i-get-started)
  * [StatTag](#stattag)
  * [StatTag Design Decisions](#design-decisions)

[How Can I Contribute?](#how-can-i-contribute)
  * [Reporting Bugs](#reporting-bugs)
  * [Suggesting Enhancements](#suggesting-enhancements)
  * [Your First Code Contribution](#your-first-code-contribution)
  * [Pull Requests](#pull-requests)
  
[Acknowledgements](#acknowledgements)

## Code of Conduct

This project and everyone participating in it is governed by the [StatTag Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to [stattag@northwestern.edu](mailto:stattag@northwestern.edu).

## I don't want to read this whole thing I just have a question!!!

Feel free to [open an issue](issues) or reach out to [stattag@northwestern.edu](mailto:stattag@northwestern.edu).

## What should I know before I get started?

### StatTag

[StatTag](http://stattag.org) is a free, open-source software plug-in for conducting reproducible research. It facilitates the creation of dynamic documents using Microsoft Word documents and statistical software, such as Stata, SAS and R. Users can use StatTag to embed statistical output (estimates, tables and figures) into a Word document and then with one click individually or collectively update output with a call to the statistical program. What makes StatTag different from other tools for creating dynamic documents is that it allows for statistical code to be edited directly from Microsoft Word.  Using StatTag means that modifications to a dataset or analysis no longer require transcribing or re-copying results into a manuscript or table.

The StatTag project is comprised of a Word add-in for Windows, or a standalone application for macOS.  Additional supporting libraries exist to support StatTag, but are intended to be reused within other projects.  Recognizing that the design objectives of StatTag may have influenced how reusable these components are, we welcome contributions to generalize all software components for other uses.


### Design Decisions

We have tried to record design details, decisions, and rationale within the [StatTag/stattag-documentation repository](https://github.com/StatTag/stattag-documentation). If you have a question around how we do things, check to see if it is documented there. If it is *not* documented there, please feel free to open a new issue in the respective repository.

## How Can I Contribute?

### Reporting Bugs

We do our best to make StatTag as solid as possible, but we realize that bugs still get through.  Currently the majority of bugs are reported via our project e-mail: [StatTag@northwestern.edu](mailto:StatTag@northwestern.edu).  However, we do welcome bug reports via repository issues as well.

#### How Do I Submit A (Good) Bug Report?

Explain the problem and include additional details to help maintainers reproduce the problem.  Include details about your configuration and environment:

* **Which version of StatTag are you using?** You can get the exact version from the 'About' menu.
* **What's the name and version of the OS you're using**? Please include if it is a 64-bit or 32-bit version of the OS (for Windows).
* **Which statistical software do you have installed?** Provide name, version, and if it is a 64-bit or 32-bit version.
* **Which version of Word do you have installed?** Provide name, version, and if it is a 64-bit or 32-bit version.  _Please note that this can be different on Windows from the OS - you can run a 64-bit version of the OS, and a 32-bit version of Word._
* **Do you have administrative rights on your machine?**

### Suggesting Enhancements

StatTag has improved over the years based on feedback from its users.  While we cannot make every change requested, we love hearing what features you believe would make StatTag more usable.  Similar to bugs, most enhancement requests are reported via our project e-mail: [StatTag@northwestern.edu](mailto:StatTag@northwestern.edu).

#### How Do I Submit A (Good) Enhancement Suggestion?

* **Use a clear and descriptive title** for the issue to identify the suggestion.
* **Describe the current behavior** and **explain which behavior you expected to see instead** and why.
* **Explain why this enhancement would be useful** to most StatTag users.

### Your First Code Contribution

Unsure where to begin contributing to StatTag? You can start by looking through `beginner` and `help-wanted` issues:

* [Beginner issues][beginner] - issues which should only require a few lines of code, and a test or two.
* [Help wanted issues][help-wanted] - issues which should be a bit more involved than `beginner` issues.

Both issue lists are sorted by total number of comments. While not perfect, number of comments is a reasonable proxy for impact a given change will have.


#### Local development

StatTag can be developed locally. For instructions on how to do this, see the respective README in each repository.

### Pull Requests

The process described here has several goals:

- Maintain StatTag's quality
- Fix problems that are important to users
- Engage the community in working toward the best possible StatTag
- Enable a sustainable system for StatTag's maintainers to review contributions

Please follow these steps to have your contribution considered by the maintainers:

1. Fork the appropriate repository and develop your change in a new feature branch.
2. Provide unit tests or describe manual integration tests and expected results.
3. Initiate a pull request through GitHub.
4. Please be patient as our maintainers review and test the change.

Please note that the reviewer(s) may ask you to complete additional design work, tests, or other changes before your pull request can be ultimately accepted.  Additionally, please understand that sometimes it is necessary to reject a pull request if it doesn't meet the overall objectives of the StatTag project.  Please feel free to reach out via GitHub issues if you'd like feedback on any proposed change before you begin.


## Acknowledgements

_This work was developed within the Northwestern University Clinical and Translational Sciences Institute, supported in part by the National Institutes of Health’s National Center for Advancing Translational Sciences (grant UL1TR001422).  The content is solely the responsibility of the developers and does not necessarily represent the official views of the National Institutes of Health or Northwestern University._
