# Requirements Document

## Introduction

This feature establishes a set of steering rules — structured guidance documents stored in `.kiro/steering/` — that help an AI assistant understand the project's product context, technical stack, and codebase structure. The three placeholder files (`product.md`, `tech.md`, `structure.md`) already exist but contain only scaffolding prompts. The goal is to define requirements for populating and maintaining these files so that any AI assistant working in this repository has accurate, actionable context at all times.

## Glossary

- **Steering_File**: A Markdown document in `.kiro/steering/` that provides persistent, always-loaded context to the AI assistant.
- **Product_File**: The steering file at `.kiro/steering/product.md` describing what the product does and who it serves.
- **Tech_File**: The steering file at `.kiro/steering/tech.md` describing the technology stack and common commands.
- **Structure_File**: The steering file at `.kiro/steering/structure.md` describing the repository layout and conventions.
- **AI_Assistant**: The AI-powered development environment (Kiro) that reads steering files to guide its behavior.
- **Developer**: A human contributor who writes, reviews, or maintains code in this repository.

---

## Requirements

### Requirement 1: Product Context Steering File

**User Story:** As a Developer, I want the product steering file to describe what the product does and who it is for, so that the AI_Assistant can give contextually relevant suggestions aligned with the product's goals.

#### Acceptance Criteria

1. THE Product_File SHALL contain a plain-language description of the product's purpose and its intended users.
2. THE Product_File SHALL list the key features and goals of the product.
3. THE Product_File SHALL define any domain-specific terminology or concepts relevant to the product.
4. WHEN the product's purpose, audience, or key features change, THE Product_File SHALL be updated to reflect those changes before the next AI_Assistant session.
5. IF the Product_File contains only placeholder text, THEN THE AI_Assistant SHALL treat the product context as unknown and request clarification from the Developer before making product-scoped decisions.

---

### Requirement 2: Technology Stack Steering File

**User Story:** As a Developer, I want the tech steering file to accurately describe the project's technology stack and common commands, so that the AI_Assistant uses the correct languages, frameworks, and tooling in all generated code and suggestions.

#### Acceptance Criteria

1. THE Tech_File SHALL specify the programming language(s) and their runtime versions used in the project.
2. THE Tech_File SHALL list the frameworks and key libraries the project depends on.
3. THE Tech_File SHALL identify the build tools and package managers in use.
4. THE Tech_File SHALL identify the test framework(s) in use.
5. THE Tech_File SHALL include a "Common Commands" section with the exact commands for installing dependencies, building the project, running tests, and starting a development server.
6. WHEN a new dependency, tool, or runtime version is adopted, THE Tech_File SHALL be updated before the AI_Assistant is used to generate code that depends on that addition.
7. IF the Tech_File contains only placeholder text, THEN THE AI_Assistant SHALL infer the stack from existing configuration files (e.g., `package.json`, `pyproject.toml`, `Cargo.toml`) and notify the Developer of its inference.

---

### Requirement 3: Project Structure Steering File

**User Story:** As a Developer, I want the structure steering file to describe the repository layout and naming conventions, so that the AI_Assistant places new files in the correct locations and follows established conventions.

#### Acceptance Criteria

1. THE Structure_File SHALL describe the top-level folder layout and the purpose of each directory.
2. THE Structure_File SHALL specify where source code, tests, assets, and configuration files are located.
3. THE Structure_File SHALL document any naming conventions for files and modules.
4. WHERE the project uses a monorepo layout, THE Structure_File SHALL describe the package or workspace boundaries.
5. WHEN a new top-level directory is added to the repository, THE Structure_File SHALL be updated to include that directory and its purpose.
6. IF the Structure_File contains only placeholder text, THEN THE AI_Assistant SHALL infer the structure from the existing file tree and notify the Developer of its inference.

---

### Requirement 4: Steering File Consistency

**User Story:** As a Developer, I want all three steering files to use consistent terminology, so that the AI_Assistant does not receive contradictory guidance across files.

#### Acceptance Criteria

1. THE Steering_File set SHALL use the same name for each technology, tool, or concept across all three files.
2. WHEN a term is introduced in one Steering_File, THE AI_Assistant SHALL apply that term consistently when referencing the same concept in responses.
3. IF a contradiction is detected between two Steering_Files, THEN THE AI_Assistant SHALL surface the conflict to the Developer and request resolution before proceeding.

---

### Requirement 5: Steering File Completeness Validation

**User Story:** As a Developer, I want to know when a steering file is incomplete, so that I can fill in missing information before relying on AI_Assistant guidance.

#### Acceptance Criteria

1. THE AI_Assistant SHALL identify a Steering_File as incomplete WHEN it contains only the original placeholder prompts and no substantive content.
2. WHEN a Steering_File is identified as incomplete, THE AI_Assistant SHALL list the specific sections that are missing content.
3. THE AI_Assistant SHALL proceed with best-effort inference WHEN a Steering_File is incomplete, and SHALL clearly indicate which parts of its response are inferred rather than grounded in the steering content.
