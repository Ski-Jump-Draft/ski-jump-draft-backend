# Ski Jump Draft

Ski Jump Draft is an online game for ski jumping fans. This repository contains the server-side code.

The project is currently **closed**.

It was hosted in October 2025 (beta version) on Fly.io, with the frontend on Vercel. Around 10 players took part in tests.

## Short technical summary
- Backend: ASP.NET (C#) with elements of F#
- Architecture: strong focus on **Dependency Inversion Principle (DIP)**  + Domain Driven Design (DDD)  
- CQRS approach (partially applied)
- Real-time communication using SignalR 

## Architectural overview

Project is split into a few parts:

- **Domain Layer** (F#) — contains the core logic, fully independent from infrastructure.  
  It defines rules like draft order or which jumpers advance to the next round.

- **Application Layer** (C#) — orchestration layer.  
  Handles workflows like scheduling phases or moving the draft forward after timeouts.

- **Infrastructure Layer** (C#) — implementations of contracts from Domain and Application.  
  Includes persistence, schedulers, messaging, etc.

- **Web Layer** (C#, ASP.NET)** — API layer with Dependency Injection and entry point (Program.cs).  
  This is the most framework-dependent part.

- **Ski Jumping Simulator Layer** — custom jump simulation engine, based on probability and designed specifically for this game.  
  Planned to be extracted into a separate web service.

## Architecture rules

Ski Jump Draft is based on:

- SOLID principles (especially Dependency Inversion Principle)  
- Uncle Bob's Clean Architecture rules (e.g. use cases)
- Selected Domain Driven Design concepts:
  - entities  
  - value objects  
  - bounded contexts

## Domain components (Bounded Contexts)

- **Game** — main core of the system  
  Handles game phases, draft logic, and ranking rules  

- **Competition** — ski jumping competition logic  
  Currently coupled with Game

- **Matchmaking** — gathers players before the game starts  

- **Simulation** — defines interfaces for jump simulation, judges, and weather  

- **GameWorld** — global data (jumpers, hills), planned to include records and events  

## Quickstart

1. Copy `.env.example` → `.env`  
2. Fill in environment variables

## Screenshots
<img width="1832" height="1028" alt="image" src="https://github.com/user-attachments/assets/97fd684e-99d6-49c9-b084-678398f26507" />
<img width="2097" height="1062" alt="image" src="https://github.com/user-attachments/assets/83cab20b-5f3f-43c3-b1bd-0f434594a7d0" />
<img width="2097" height="1113" alt="image" src="https://github.com/user-attachments/assets/11ef5bd3-6c52-43e0-88f4-3176ed48c1f3" />
<img width="1835" height="1167" alt="image" src="https://github.com/user-attachments/assets/1f3eb7ac-73ba-453e-bf52-e80617b5b493" />
