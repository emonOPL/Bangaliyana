<div align="center">

# 🛒 Bangaliyana

### _Bangladesh's Premier Multi-Vendor E-Commerce Platform_

[![.NET](https://img.shields.io/badge/.NET-7.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Entity Framework](https://img.shields.io/badge/Entity_Framework-Core_7-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://docs.microsoft.com/ef/core)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2019+-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://docs.microsoft.com/aspnet/signalr)

[![License](https://img.shields.io/badge/License-Proprietary-red?style=flat-square)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-Welcome-brightgreen?style=flat-square)](CONTRIBUTING.md)
[![Maintenance](https://img.shields.io/badge/Maintained-Yes-green?style=flat-square)](https://github.com/emonOPL/Bangaliyana)

<br/>

[🌐 **Live Demo**](https://bangaliyana.bsite.net) • [📖 **Documentation**](#-documentation) • [🚀 **Quick Start**](#-quick-start) • [💡 **Features**](#-features)

<br/>

<img src="https://raw.githubusercontent.com/andreasbm/readme/master/assets/lines/rainbow.png" alt="line" width="100%">

</div>

## 📋 Table of Contents

<details>
<summary>Click to expand</summary>

- [Overview](#-overview)
- [Live Demo](#-live-demo)
- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Quick Start](#-quick-start)
- [Configuration](#-configuration)
- [Services](#-services)
- [User Roles](#-user-roles)
- [Security](#-security)
- [Contributing](#-contributing)
- [Contact](#-contact)

</details>

---

## 🎯 Overview

**Bangaliyana** is a comprehensive, feature-rich multi-vendor e-commerce platform built specifically for the Bangladeshi market. Built with cutting-edge technologies like ASP.NET Core 7.0, Entity Framework Core, and SignalR, it delivers a seamless shopping experience with real-time updates, biometric authentication, and intelligent features.

<div align="center">

### 🌟 Why Bangaliyana?

</div>

|                 🎨 Modern UI                  |           ⚡ Real-time            |                 🔒 Secure                  |              🇧🇩 Localized              |
| :-------------------------------------------: | :-------------------------------: | :----------------------------------------: | :------------------------------------: |
| Beautiful, responsive design with Bootstrap 5 | Instant notifications via SignalR | WebAuthn biometric auth & security headers | Full Bangladesh address system support |

---

## 🌐 Live Demo

<div align="center">

### 👉 [https://bangaliyana.bsite.net](https://bangaliyana.bsite.net) 👈

<br/>

| 🔐 **Demo Credentials** |                     |
| :---------------------- | :------------------ |
| **Admin Email**         | `emontwo@gmail.com` |
| **Password**            | `Emon@123`          |

</div>

---

## ✨ Features

<div align="center">

### 🛍️ Customer Experience

</div>

<table align="center">
<tr>
<td width="50%">

#### 🔍 Shopping & Discovery

- ✅ Advanced product search with filters
- ✅ Category & subcategory browsing
- ✅ Product comparison (side-by-side)
- ✅ Wishlist management
- ✅ Search history tracking
- ✅ Price drop alerts
- ✅ Flash sales & deals

</td>
<td width="50%">

#### 🛒 Cart & Checkout

- ✅ Hybrid cart (guest + logged-in)
- ✅ Multiple payment gateways
- ✅ bKash, Nagad, Upay integration
- ✅ SSLCommerz online payment
- ✅ Cash on Delivery (COD)
- ✅ District-wise delivery charges
- ✅ Order tracking

</td>
</tr>
<tr>
<td width="50%">

#### ⭐ Engagement

- ✅ Product reviews & ratings
- ✅ Q&A on products
- ✅ Seller messaging
- ✅ Real-time notifications
- ✅ Email notifications
- ✅ Newsletter subscription

</td>
<td width="50%">

#### 🎁 Rewards & Benefits

- ✅ Reward points system
- ✅ Premium membership
- ✅ Exclusive discounts
- ✅ Coupon codes
- ✅ Referral bonuses
- ✅ Loyalty rewards

</td>
</tr>
</table>

<div align="center">

### 🏪 Seller Tools

</div>

<table align="center">
<tr>
<td width="50%">

#### 📊 Dashboard & Analytics

- ✅ Sales overview & statistics
- ✅ Revenue analytics
- ✅ Order management
- ✅ Monthly performance reports
- ✅ Customer insights
- ✅ Product performance

</td>
<td width="50%">

#### 📦 Product & Inventory

- ✅ Product listing management
- ✅ Bulk product import (Excel)
- ✅ Inventory tracking
- ✅ Low stock alerts
- ✅ Price management
- ✅ Variant support (size, color)

</td>
</tr>
<tr>
<td width="50%">

#### 💬 Communication

- ✅ Customer messaging
- ✅ Review responses
- ✅ Q&A management
- ✅ Support tickets

</td>
<td width="50%">

#### 💰 Payments

- ✅ Earnings dashboard
- ✅ Payout management
- ✅ Transaction history
- ✅ Bank account setup
- ✅ Payment reports

</td>
</tr>
</table>

<div align="center">

### 👨‍💼 Admin Panel

</div>

<table align="center">
<tr>
<td width="33%">

#### 👥 User Management

- ✅ User CRUD operations
- ✅ Role management
- ✅ Permission control
- ✅ Seller approvals
- ✅ User analytics

</td>
<td width="33%">

#### 🛍️ Store Management

- ✅ Category management
- ✅ Product moderation
- ✅ Order oversight
- ✅ Coupon management
- ✅ Flash sales

</td>
<td width="33%">

#### 🎨 CMS & Settings

- ✅ Banner management
- ✅ Dynamic pages
- ✅ Menu configuration
- ✅ Site settings
- ✅ Social links

</td>
</tr>
</table>

<div align="center">

### 🔧 Technical Features

<br/>

| Feature                  | Technology     | Description                          |
| :----------------------- | :------------- | :----------------------------------- |
| 🔐 **Biometric Auth**    | WebAuthn/Fido2 | Fingerprint & face recognition login |
| ⚡ **Real-time Updates** | SignalR        | Instant notifications, live chat     |
| 📋 **Background Jobs**   | Hangfire       | Scheduled tasks, payment processing  |
| 📄 **PDF Generation**    | QuestPDF       | Order receipts, reports              |
| 📊 **Excel Export**      | ClosedXML      | Data export functionality            |
| 🤖 **AI Chat**           | Custom Service | AI-powered customer support          |

</div>

---

## 🛠️ Tech Stack

<div align="center">

### Backend

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)

### Frontend

![HTML5](https://img.shields.io/badge/HTML5-E34F26?style=for-the-badge&logo=html5&logoColor=white)
![CSS3](https://img.shields.io/badge/CSS3-1572B6?style=for-the-badge&logo=css3&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![jQuery](https://img.shields.io/badge/jQuery-0769AD?style=for-the-badge&logo=jquery&logoColor=white)

### Tools & Services

![SignalR](https://img.shields.io/badge/SignalR-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Hangfire](https://img.shields.io/badge/Hangfire-4A154B?style=for-the-badge&logo=hangfire&logoColor=white)
![Chart.js](https://img.shields.io/badge/Chart.js-FF6384?style=for-the-badge&logo=chart.js&logoColor=white)

### Payment Gateways

![SSLCommerz](https://img.shields.io/badge/SSLCommerz-00A651?style=for-the-badge&logoColor=white)
![bKash](https://img.shields.io/badge/bKash-D12053?style=for-the-badge&logoColor=white)
![Nagad](https://img.shields.io/badge/Nagad-F6921E?style=for-the-badge&logoColor=white)

</div>

---

## 🏗️ Architecture

<details>
<summary><b>📁 Project Structure</b> (Click to expand)</summary>

```
Bangaliyana/
│
├── 📂 Areas/
│   ├── 📂 Admin/              # 👨‍💼 Admin panel
│   │   ├── Controllers/       # 31 controllers
│   │   └── Views/             # Admin views
│   │
│   ├── 📂 Customer/           # 🛍️ Customer features
│   │   ├── Controllers/       # 11 controllers
│   │   └── Views/             # Customer views
│   │
│   ├── 📂 Seller/             # 🏪 Seller dashboard
│   │   ├── Controllers/       # 11 controllers
│   │   └── Views/             # Seller views
│   │
│   ├── 📂 Moderator/          # 👮 Moderation tools
│   │   ├── Controllers/       # 9 controllers
│   │   └── Views/             # Moderator views
│   │
│   └── 📂 Identity/           # 🔐 Auth UI
│       └── Pages/             # Identity pages
│
├── 📂 Controllers/            # 🎮 API controllers
├── 📂 Data/                   # 💾 DbContext & migrations
├── 📂 Extensions/             # 🔧 Extension methods
├── 📂 Filters/                # 🎯 Action filters
├── 📂 Hubs/                   # 📡 SignalR hubs
├── 📂 Middleware/             # ⚙️ Custom middleware
├── 📂 Models/                 # 📊 60+ domain entities
├── 📂 Services/               # 🔄 40+ business services
├── 📂 Utilities/              # 🛠️ Helper classes
├── 📂 Views/                  # 🖼️ Shared views
└── 📂 wwwroot/                # 📁 Static assets
    ├── css/
    ├── js/
    └── images/
```

</details>

<details>
<summary><b>🗃️ Database Schema Highlights</b> (Click to expand)</summary>

#### 🇧🇩 Bangladesh Address Hierarchy

```
Division → District → Upazila → Union
```

#### 📊 Key Entities

| Entity            | Description                      |
| :---------------- | :------------------------------- |
| `ApplicationUser` | Extended identity with BD fields |
| `Products`        | Product catalog with variants    |
| `Orders`          | Order management                 |
| `Seller`          | Seller profiles & shops          |
| `Category`        | Product categorization           |
| `PersistentCart`  | Database cart storage            |

</details>

---

## 🚀 Quick Start

### Prerequisites

| Requirement                                                                                      | Version |
| :----------------------------------------------------------------------------------------------- | :------ |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/7.0)                                     | 7.0+    |
| [SQL Server](https://www.microsoft.com/sql-server)                                               | 2019+   |
| [Visual Studio](https://visualstudio.microsoft.com/) / [VS Code](https://code.visualstudio.com/) | Latest  |

### Installation Steps

```bash
# 1️⃣ Clone the repository
git clone https://github.com/emonOPL/Bangaliyana.git
cd Bangaliyana

# 2️⃣ Update database connection in appsettings.json
# See Configuration section below

# 3️⃣ Run database migrations
dotnet ef database update

# 4️⃣ Build the project
dotnet build

# 5️⃣ Run the application
dotnet run

# 🎉 Access at: https://localhost:5005
```

### 🐳 Docker (Coming Soon)

```bash
docker-compose up -d
```

---

## ⚙️ Configuration

<details>
<summary><b>📝 appsettings.json</b> (Click to expand)</summary>

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Bangaliyana;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "User": "your-email@gmail.com",
    "Pass": "your-app-password"
  },
  "Checkout": {
    "DeliveryCharge": 100.0
  },
  "AppSettings": {
    "DefaultPageSize": 12,
    "AdminPageSize": 20,
    "SessionTimeoutMinutes": 30
  },
  "IdentitySettings": {
    "LockoutTimeSpanMinutes": 5,
    "MaxFailedAccessAttempts": 5,
    "PasswordMinLength": 6
  }
}
```

</details>

<details>
<summary><b>🔧 Production Configuration</b> (Click to expand)</summary>

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Production-Connection-String"
  },
  "Hangfire": {
    "Enabled": false
  },
  "BackgroundServices": {
    "Enabled": false
  },
  "Fido2": {
    "ServerDomain": "your-domain.com",
    "ServerName": "Bangaliyana",
    "Origins": ["https://your-domain.com"]
  }
}
```

</details>

---

## 🔄 Services

<div align="center">

| Service                           | Purpose                  |
| :-------------------------------- | :----------------------- |
| 🛒 `ICartService`                 | Unified cart operations  |
| 📧 `IEmailService`                | Email notifications      |
| 🔍 `ISearchService`               | Product search + history |
| 📱 `IOtpService`                  | OTP verification         |
| 📋 `IMenuService`                 | Dynamic menu management  |
| 📄 `PdfGeneratorService`          | PDF receipts             |
| 🎁 `IRewardService`               | Reward points system     |
| 🔔 `INotificationService`         | User notifications       |
| 💰 `ISellerPaymentService`        | Seller payouts           |
| 📢 `IPromotionalCampaignService`  | Marketing campaigns      |
| ⭐ `IShopRatingService`           | Shop ratings             |
| 📡 `IRealTimeNotificationService` | SignalR notifications    |
| 🤖 `IAIChatService`               | AI chat support          |
| 📝 `IBlogService`                 | Blog management          |
| 📊 `IAuditService`                | Activity logging         |

</div>

---

## 👥 User Roles

<div align="center">

| Role           | Icon | Access Level                      |
| :------------- | :--: | :-------------------------------- |
| **SuperAdmin** |  👑  | Full system access, manage admins |
| **Admin**      |  👨‍💼  | Products, orders, users, content  |
| **Moderator**  |  👮  | Content moderation, support       |
| **Seller**     |  🏪  | Own shop & products               |
| **User**       |  👤  | Shopping & account                |

</div>

---

## 🔒 Security

<table>
<tr>
<td width="50%">

#### 🛡️ Authentication

- ✅ ASP.NET Core Identity
- ✅ WebAuthn/Fido2 biometric
- ✅ Two-factor authentication
- ✅ Account lockout protection
- ✅ Password policies

</td>
<td width="50%">

#### 🔐 Protection

- ✅ CSRF tokens
- ✅ XSS prevention
- ✅ SQL injection protection
- ✅ Rate limiting
- ✅ Security headers

</td>
</tr>
</table>

---

## 📊 API Endpoints

| Endpoint        | Description                       |
| :-------------- | :-------------------------------- |
| `GET /health`   | System health check               |
| `GET /hangfire` | Background jobs dashboard (Admin) |

---

## 🤝 Contributing

We welcome contributions! Here's how you can help:

1. 🍴 **Fork** the repository
2. 🌿 **Create** a feature branch (`git checkout -b feature/AmazingFeature`)
3. 💾 **Commit** your changes (`git commit -m 'Add AmazingFeature'`)
4. 📤 **Push** to the branch (`git push origin feature/AmazingFeature`)
5. 🔃 **Open** a Pull Request

---

## 📞 Contact

<div align="center">

**Developed with ❤️ by Emon**

[![GitHub](https://img.shields.io/badge/GitHub-@jfemon8-181717?style=for-the-badge&logo=github)](https://github.com/jfemon8)
[![Email](https://img.shields.io/badge/Email-jfemon8@gmail.com-EA4335?style=for-the-badge&logo=gmail&logoColor=white)](mailto:jfemon8@gmail.com)

</div>

---

## 🙏 Acknowledgments

<div align="center">

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://docs.microsoft.com/aspnet/core)
[![Entity Framework](https://img.shields.io/badge/Entity_Framework-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://docs.microsoft.com/ef/core)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?style=flat-square&logo=bootstrap&logoColor=white)](https://getbootstrap.com)
[![SignalR](https://img.shields.io/badge/SignalR-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://docs.microsoft.com/aspnet/signalr)
[![Hangfire](https://img.shields.io/badge/Hangfire-4A154B?style=flat-square)](https://www.hangfire.io)
[![QuestPDF](https://img.shields.io/badge/QuestPDF-00A2FF?style=flat-square)](https://www.questpdf.com)

</div>

---

<div align="center">

<img src="https://raw.githubusercontent.com/andreasbm/readme/master/assets/lines/rainbow.png" alt="line" width="100%">

### ⭐ Star this repo if you find it helpful!

<sub>Copyright © 2026 Bangaliyana. All rights reserved.</sub>

</div>
