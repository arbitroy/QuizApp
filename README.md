# QuizApp

A full-featured web application for creating, managing, and taking quizzes built with ASP.NET Core Razor Pages.

## Features

### User Features

- Take quizzes on various topics
- Time-limited quiz sessions
- Review quiz results and scores
- Track learning progress through the dashboard
- Account management

### Administrator Features

- Create and manage quizzes with a rich editor
- Add multiple-choice questions with customizable options
- Set time limits for quizzes
- User management (create, edit, delete users)
- Reset user passwords
- View activity and statistics

## Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Frontend**: HTML, CSS, JavaScript, Bootstrap 5
- **Data Access**: Entity Framework Core 9.0
- **Database**: Microsoft SQL Server (LocalDB)
- **Authentication**: ASP.NET Core Identity

## Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or later (recommended)
- SQL Server LocalDB (included with Visual Studio)

## Installation

1. Clone the repository:

   ```
   git clone https://github.com/yourusername/QuizApp.git
   ```
2. Navigate to the project directory:

   ```
   cd QuizApp
   ```
3. Restore dependencies:

   ```
   dotnet restore
   ```
4. Apply database migrations:

   ```
   dotnet ef database update
   ```
5. Run the application:

   ```
   dotnet run
   ```
6. Open your browser and navigate to `https://localhost:5063` or `http://localhost:5063`

## Initial Setup

The application will automatically create the database and seed it with:

- A default admin user:

  - Email: admin@quizapp.com
  - Password: Admin123!
- A sample quiz with basic questions

## Project Structure

- **/Data**: Contains database context and data seeding logic
- **/Models**: Entity classes representing the domain model
- **/Pages**: Razor Pages for the user interface
  - **/Admin**: Administrator area
    - **/Dashboard**: User statistics and information
    - **/Quizzes**: Quiz management (create, edit, delete)
    - **/Users**: User management
  - **/Quizzes**: Quiz-taking functionality
    - **Index.cshtml**: Available quizzes
    - **Take.cshtml**: Quiz-taking interface
    - **Result.cshtml**: Quiz results display

## User Roles

The application supports two roles:

1. **User**: Can take quizzes and view their own results
2. **Administrator**: Full access to create/edit quizzes and manage users

## Development Notes

### Database Schema

- **Quiz**: Basic quiz information (title, description, time limit)
- **Question**: Quiz questions
- **Option**: Answer options for questions
- **QuizAttempt**: Records of quiz sessions
- **UserAnswer**: User's selected answers

### Authorization

Authorization is handled through ASP.NET Core Identity with policy-based authorization:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
});
```

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Commit your changes: `git commit -m 'Add some feature'`
4. Push to the branch: `git push origin feature-name`
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgements

- ASP.NET Core team for the excellent web framework
- Bootstrap team for the frontend framework
- Entity Framework Core team for the ORM
