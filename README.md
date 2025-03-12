# 🏓 RallyCasts

![RallyCasts](https://img.shields.io/badge/RallyCasts-Pickleball%20Social%20Platform-blue)

## 🎯 About

RallyCasts is a social platform for pickleball players! 🏓 Share your matches, rallies, and highlights with the community. Connect with fellow players, join events, and even hire on-demand casters to record your games professionally.

Think of it as a specialized social media platform where pickleball enthusiasts can share content, interact, and elevate their game experience.

## ✨ Key Features

- 📱 **Social Platform**: Create profiles, follow players, and build your pickleball network
- 🎬 **Video Content**: Upload and share your best pickleball moments
- 📸 **Photo Sharing**: Post photos from tournaments, practice sessions, and events
- 👍 **Community Engagement**: Like, comment, and interact with other players' content
- 💬 **Direct Messaging**: Connect privately with the pickleball community
- 📅 **Events**: Join and organize pickleball events with fellow enthusiasts
- 🎥 **On-Demand Casters**: Hire 1099 contractors with smartphones/cameras and gimbals/tripods to professionally record your matches

## 🎥 Caster Program

A unique feature of RallyCasts is our on-demand Caster program:

- **What is a Caster?** Independent contractors equipped with smartphones/cameras and stabilization equipment who can be hired to record pickleball matches
- **How it works**: Players can request a Caster through the platform for specific events or matches
- **Quality Content**: Get professionally recorded footage of your games without having to worry about setting up equipment
- **Become a Caster**: Use your recording equipment to earn money filming pickleball matches

## 🏗️ Tech Stack

RallyCasts leverages modern technologies for a seamless experience:

### Frontend 🖥️
- **Next.js**: React framework with React Query for efficient data fetching
- **shadcn/ui**: Component library for sleek UI design
- **TailwindCSS**: Utility-first CSS framework for responsive design

### Backend 🔧
- **C# .NET**: Robust API and business logic layer
- **AWS Infrastructure**:
  - **Aurora Serverless**: PostgreSQL-compatible database for primary data storage
  - **DynamoDB**: NoSQL database for caster requests and high-throughput operations
  - **Cognito**: User authentication and management
  - **S3**: Storage solution for photos and videos
  - **SNS/SQS**: Notification system for real-time updates

## 🧩 Repository Structure

```
├── apps/
│   ├── api/            # .NET C# backend API
│   ├── api.Tests/      # Backend tests
│   └── web/            # Next.js frontend (coming soon)
├── packages/
│   ├── ui/             # Shared UI components
│   ├── eslint-config/  # ESLint configuration
│   └── typescript-config/ # TypeScript configuration
├── scripts/            # Utility scripts
├── terraform/          # Infrastructure as Code
└── localstack-init/    # Local AWS development setup
```

## 🔮 Future Enhancements

- **Payment Processing**: Stripe integration for caster payments and premium features
- **Enhanced Analytics**: Detailed insights for players and casters
- **Mobile Applications**: Native iOS and Android apps
- **AI-Powered Highlights**: Automatic generation of highlight reels from recorded matches

## 📝 Project Status

This repository showcases the backend infrastructure and API for RallyCasts. The frontend implementation with Next.js is currently under development and will be integrated soon.

---

🏓 RallyCasts - Elevating the Pickleball Experience 🏓
