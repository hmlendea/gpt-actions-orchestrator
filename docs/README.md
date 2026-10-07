# GPT Actions Orchestrator — Technical Documentation

This directory contains implementation-grounded technical documentation for the GPT Actions Orchestrator repository. These documents complement the root `ARCHITECTURE.md` by providing deeper implementation detail, execution tracing, and cross-referenced component maps.

## Document Index

| Document | Purpose |
|----------|---------|
| [Architecture Overview](architecture-overview.md) | High-level system context, architectural style, and component relationships |
| [Runtime Execution Flows](runtime-flows.md) | Detailed sequence diagrams and causal execution traces for each action |
| [Component Reference](component-reference.md) | Per-component responsibilities, dependencies, lifetimes, and source locations |
| [Integration Adapters](integration-adapters.md) | GitHub, Personal Log Manager, and Steam Storefront adapter implementations |
| [Orchestration Engine](orchestration-engine.md) | ActionsOrchestrator parameter building, alias resolution, and dispatch logic |
| [API Boundary](api-boundary.md) | Controller, middleware pipeline, request/response models, and authorisation |
| [Configuration System](configuration-system.md) | Settings binding, precedence, secret management, and validation |
| [Data Architecture](data-architecture.md) | In-memory transformations, alias datastore, and external response models |
| [Logging and Observability](logging-observability.md) | Operation logging, log info keys, and diagnostic signal flow |
| [Error Handling](error-handling.md) | Exception propagation, middleware translation, and adapter failure semantics |
| [Testing Strategy](testing-strategy.md) | Unit test coverage, integration test architecture, and verification matrices |
| [Extension Guide](extension-guide.md) | Adding new actions, aliases, and integration adapters |
| [Source Map](source-map.md) | Complete file-to-responsibility mapping for navigation |

## Navigation Guide

- **Start here** for an overview of the documentation structure
- **Architecture Overview** for system context and high-level design
- **Runtime Execution Flows** for causal tracing of request processing
- **Component Reference** when modifying or debugging a specific class
- **Integration Adapters** when working with external API boundaries
- **Orchestration Engine** for the central dispatch and parameter logic
- **Testing Strategy** for understanding test coverage and adding tests
- **Extension Guide** when adding new capabilities

## Relationship to Root Documentation

| Root Document | Purpose | This Directory |
|---------------|---------|----------------|
| `ARCHITECTURE.md` | Architectural decisions, boundaries, and contracts | Implementation detail, execution traces, component maps |
| `README.md` | Project overview, usage, and configuration | Technical deep-dives and implementation reference |
| `SECURITY.md` | Vulnerability reporting and scope | Error handling and security boundary implementation |
| `PRIVACY.md` | Data handling and operator responsibilities | Data architecture and integration data flows |
