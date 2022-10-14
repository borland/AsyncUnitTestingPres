# Async Unit Testing approach presentation

This github repo contains slides and code for a knowledge sharing presentation I gave at Octopus Deploy.

PDF of slides: [Async Unit Testing knowledge sharing.pdf](Async%20Unit%20Testing%20knowledge%20sharing.pdf)

### How to navigate the code structure.

This git repository uses branches to step through each phase of the presentation..

Suggestion: Clone the repository, and switch to the `1-initial` branch, look at the code, then move through each branch to see the code evolve into the final state.

**Branches**

`1-initial` - this just shows the base sample code with no unit tests

`2-substitute` - this shows a failed attempt at using an off-the-shelf mocking tool (NSubstitute) to deal with the problem of mocking HTTP requests and responses

`3-traditional` - this shows a traditional "canned responses" HTTP mocking technique. The tests pass but they're actually broken

`4-traditional-recorder`, and the `4a` and `4b` branches - These show enhancing the canned response model, to record requests so we can verify that the expected requests did actually occur and weren't broken.

`5-tcs-foundation` - this shows a concept - using a queue of functions to handle, verify, and respond to requests. It doesn't form part of the final solution, but provides a core insight that helps understand the next parts.

`5a -> d` - this series of branches show the evolution towards using async to make the mock serve as a simple pipe between the system-under-test and the unit test itself. Most of the 'mocking' is now simply inline inside the unit test.