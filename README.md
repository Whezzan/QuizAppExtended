# QuizAppExtended - Made By Desirée Skönneberg and Erik Holgersson

A WPF quiz game built on **.NET 8** with support for:

- Playing quiz packs with a timer and score summary
- Creating/editing question packs (configuration mode)
- Importing questions from **OpenTDB**
- Saving questions to a shared **Question Bank**
- Persisting data to **MongoDB** (packs, categories, sessions, question bank)

## Prerequisites

Before you begin, ensure you have the following installed:

- **Windows** (WPF)
- **.NET SDK 8.x**
- **Visual Studio 2022** (recommended) with the ".NET desktop development" workload
- **MongoDB** running locally or reachable over the network
  - Default connection string used by the app: `mongodb://localhost:27017`

### MongoDB Connection String

The app reads the MongoDB connection string from the environment variable:

- `QUIZAPP_MONGO_CONN`

If this variable is not set, the app will fall back to the default connection string:

- `mongodb://localhost:27017`

## Getting Started

Follow these steps to set up and run the application locally:

1. **Start MongoDB**
   - Ensure a MongoDB server is running (either through a local installation or Docker).

2. **(Optional) Set Connection String**
   - If you are not using the default connection string, set the environment variable:
     - `QUIZAPP_MONGO_CONN` = your MongoDB connection string

3. **Restore & Run**
   - Open the solution in Visual Studio.
   - Build and run the startup project.

The app will automatically create and initialize the required MongoDB collections and indexes at startup.

## Data & Storage

The app persists data in MongoDB using the database name:

- `QuizAppDb`

### Local JSON File

Some functionality still writes a local JSON file under:

- `%LOCALAPPDATA%\Laboration_3\Laboration_3.json`

## Using the App (Overview)

The application offers the following modes:

- **Play Mode**: Choose a pack, answer timed questions, and then view results.
- **Configuration Mode**: Add, edit, or delete questions, and change pack options.
- **Import**: Fetch questions from OpenTDB into a new pack.
- **Question Bank**: Save individual questions to a shared bank and add them to packs.

![Skärmdump av QuizApp Extended](Images/QuizAppExtended.png)

## Troubleshooting

If you encounter issues, consider the following solutions:

- **Cannot Connect to MongoDB**
  - Verify that MongoDB is running and that the connection string is correct.

- **Import Returns No Questions**
  - OpenTDB may return zero results for some combinations of category and difficulty.

## License

This project is provided as-is for educational purposes.
