# Food Delivery Website

## Overview

Welcome to the MVC Food Delivery Website repository! This project is a web application developed using ASP.NET MVC, designed to facilitate online food ordering and delivery services. It provides a seamless platform for customers to browse menus, place orders, and track deliveries, while enabling restaurant owners to manage orders efficiently.

#### To see how the application looks and works, go to the "Video of the app" folder in the repository and download the raw video, as it is too large to be hosted directly on GitHub.

## Features

- **Customer Side:**
  - **Menu Browsing:** View available food items with detailed descriptions and prices.
  - **Order Placement:** Add items to the cart and place orders with ease.
  - **Order Tracking:** Monitor the status of your orders in real-time.

- **Admin Side:**
  - **Order Management:** View, process, and update order statuses.
  - **Product Management:** Manage products and ingredients.
  - **Reporting:** Generate reports on sales, popular items, and customer activity.
  - **Feedback:** View customer feedback for continuous improvement.

## Technologies Used

- **Programming Language:** C#
- **Framework:** ASP.NET MVC
- **Database:** Microsoft SQL Server
- **ORM:** Entity Framework
- **Frontend:** HTML, CSS, JavaScript
- **Styling:** Bootstrap for responsive design and Tailwind CSS I used for a single-page enhancements
- **Real-Time:** Integrated with SignalR, the website provides real-time updates on the order status and delivery progress.

## Getting Started

To get a local copy up and running, follow these steps:

1. #### **Clone the Repository:**

   ```bash
   git clone https://github.com/Melli007/ConoPizza-web.git

 
#### 2. **Configure the Backend**  

   - Locate the appsettings.json file.
   - Update the ConnectionStrings section with your own connection string. For example:  

          {      
            "ConnectionStrings": {  
              "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;UserId=YOUR_USER;Password=YOUR_PASSWORD;"  
            },  
            // ... other settings  
          }          
#### 3. **Run Database Migrations**  
 - Open the Package Manager Console (PMC) in Visual Studio.

 - Ensure the Default Project in PMC is set to the correct location.

 - Create and apply the migrations to set up your database:

- **To create a new migration:**
  ```bash
   Add-Migration InitialCreate  
- **To apply the migration and update the database:**
  ```bash
    Update-Database
 - This process creates the necessary tables in your database as defined in your entity models.
#### 4. **User Account Creation**
- When the Backend runs for the first time, it seeds an admin user into the database.
- The data seeding logic in DataSeeder.cs is designed to prevent reseeding on subsequent runs.
- To access the admin panel, navigate to:
  ```bash
    https://localhost:7030/admin/login
Then log in using:
  - **Username:** Admin 
  - **Password:** Admin123!

## Contributing
 Contributions are welcome! If you’d like to contribute, please follow these steps:

1. Fork the repository.

2. Create a new branch for your feature or bug fix.

3. Commit your changes with clear and descriptive commit messages.

4. Push your branch and open a Pull Request. 



