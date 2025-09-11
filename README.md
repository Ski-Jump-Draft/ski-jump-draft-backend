# Ski Jump Draft
This repository contains the server-side code for [Ski Jump Draft] online game for ski jumping enthusiasts. It's planned to be hosted on Fly.io platform.

## Contribution
🚧 The project is not yet open for feature contributions.  
We accept only **code quality / documentation improvements** for now.  
Bug reports and questions are welcome via GitHub Issues.  
See [CONTRIBUTING.md](./CONTRIBUTING.md).

## Architectural overview
Project is split into a few parts:
- **Domain Layer** (F#) — contains the core logic which is totally independent of technical details, like where does the app run or which database system do we use. For example, it determines the order in Draft or which jumpers will advance to next competition's round.
- **Application Layer** (C#) — as Uncle Bob said, this is an "automation" layer. For example, it schedules next phases in the game or draft pass after a timeout.
- **Infrastructure Layer** (C#) — includes implementations of contracts contained in Domain and Application layers; it has the logic of persisting the data, ORM's, schedulers, buses and so on.
- **Web Layer** (C#, ASP.NET) — includes a Web API, Dependency Injection and main Program.cs file. Most "dirty" part of the project.
- **Ski Jumping Simulator Layer** — includes a jumps simulator written especially for SJ Draft, which is planned to become a web service.
## Architecture rules
Ski Jump Draft heavily relies on:
- SOLID principles, especially **Dependency Inversion Principle**
- Uncle Bob's "Clean Architecture" book
- Certain concepts from Domain Driven Design (entities, value objects, bounded contexts)
## Domain components (aka Bounded Contexts)
- Game (main axis of project, manages the transitions between phases, has settings, manages the draft logic and game ranking calculation policy)
- Competition (for now, game "copies" the _Competition_ component logic without contexts separation — planned to refactor)
- Matchmaking (logic for gathering players before the game)
- Simulation (defines jump simulation, judge evaluation and weather engine interfaces used across the project, which are implemented in a separate project)
- GameWorld (currently it holds global jumpers and hills "pack". It's planned to manage some records, stats and events which will spice up the game for all users)

## Quickstart
1. Copy `.env.example` → `.env`
<To fill up!>
