# Agent guidelines

## Code style

- XML doc comments are fine when they add information the name and signature don't (contract, units, exceptions, side effects).
- Do not write narrative comments that describe what the code does or how it got there.
- Leave existing comments as they are.

## API methods

- One method per request. Don't keep sync and async variants side by side; use the async one with the `Async` suffix.
