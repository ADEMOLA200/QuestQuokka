### **📜 QuestQuokka - Discord Fun & Games Bot**  

🚀 **QuestQuokka** is a **feature-rich Discord bot** built in **C# with Discord.Net** that brings interactive games and fun to your server! 🎮  
It currently features **Tic-Tac-Toe** with real-time button interactions and will be expanded with more multiplayer games.  

---

## **🎮 Games Currently Added**

- **Tic-Tac-Toe (Button-Based)**  
  Engage in interactive, real-time Tic-Tac-Toe games using Discord buttons. Players take turns, and the bot automatically detects wins, draws, and prevents out-of-turn moves.

- **Trivia**  
  Answer dynamically fetched trivia questions from OpenTDB. Options are shuffled and embedded in buttons with built-in answer validation, letting you know instantly if your answer is correct or incorrect.
 
---

## **📌 Features**  

✔️ **Tic-Tac-Toe (Button-Based)** – Play an interactive game using Discord buttons!  
✔️ **Turn-Based System** – Players take turns, enforced by the bot.  
✔️ **Win & Draw Detection** – The bot automatically checks for winners or ties.  
✔️ **Real-Time Board Updates** – The game board updates after every move.  
✔️ **Prevents Cheating** – Players cannot make moves out of turn.  
✔️ **Planned Expansions** – More games like Chess, Sudoku, and Trivia!  

---

## **🛠️ Installation & Setup**  

### **1️⃣ Prerequisites**  
Before running the bot, ensure you have:  
- ✅ **.NET 6+** installed ([Download .NET](https://dotnet.microsoft.com/en-us/download))  
- ✅ A **Discord Bot Token** ([Create one here](https://discord.com/developers/applications))  
- ✅ **Admin Permissions** to invite the bot to your server  

---

### **2️⃣ Clone the Repository**  
```sh
git clone https://github.com/ADEMOLA200/QuestQuokka.git
cd QuestQuokka
```

---

### **3️⃣ Install Dependencies**  
Run the following command to install **Discord.Net**:  
```sh
dotnet add package Discord.Net
dotnet add package Discord.Net.Commands
dotnet add package Discord.Net.WebSocket
dotnet add package Discord.Net.Interactions
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Newtonsoft.Json
dotnet add package Microsoft.Extensions.Configuration.Json
```

---

### **4️⃣ Configure the Bot Token**  
Create a new file **`appsettings.json`** in the root directory and paste:  
```json
{
  "DiscordBot": {
    "Token": "YOUR_DISCORD_BOT_TOKEN",
    "Prefix": "!",
    "DailyReward": 100
  }
}
```
🔹 Replace `"YOUR_DISCORD_BOT_TOKEN"` with your actual **Discord bot token**.  

---

### **5️⃣ Run the Bot**  
Start the bot using:  
```sh
dotnet run
```
Once the bot is running, **invite it to your server** and start playing! 🎉  

---

## **🎮 How to Use the Bot**  

### **▶️ Start a Tic-Tac-Toe Game**  
```sh
!tictactoe @opponent
```
🔹 **Mentions a user** and starts a **3x3 button-based game**.  
🔹 Players **click buttons** to make their moves.  
🔹 The bot **updates the board** after every turn.  
🔹 The game **automatically detects winners and ties**.  

---

## **🛠️ Future Enhancements**  

🚀 **Planned Features**:  
✅ **More Games** – Chess, Sudoku, Rock-Paper-Scissors  
✅ **Leaderboard System** – Track player wins/losses  
✅ **Game Time Limits** – Auto-forfeit inactive players  
✅ **Multiplayer Expansions** – Tournaments & rankings  

---

## **🤝 Contributing**  

We welcome contributions! Follow these steps:  
1. **Fork** the repo  
2. Create a **feature branch** (`git checkout -b feature-name`)  
3. **Commit your changes** (`git commit -m "Added feature XYZ"`)  
4. **Push to your branch** (`git push origin feature-name`)  
5. Open a **Pull Request** 🚀  

---

## **📜 License**  

📄 This project is **open-source** under the **MIT License**.  

---

## **📞 Contact & Support**  

👨‍💻 **Created by:** *ADEMOLA200*  
📧 Email: odukoyaabdullahi01@gmail.com
🔗 GitHub: [ADEMOLA200](https://github.com/ADEMOLA200)  

---

🎮 **QuestQuokka – Bringing fun to your Discord server!** 🦘🚀  
