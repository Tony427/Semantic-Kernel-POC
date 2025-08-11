# Setup Guide

## OpenAI API Configuration

### 1. Get OpenAI API Key
1. Visit [OpenAI Platform](https://platform.openai.com/api-keys)
2. Create a new API key
3. Copy the API key

### 2. Configure Local Settings
1. Copy `appsettings.Development.example.json` to `appsettings.Development.json`
2. Replace `sk-your-actual-openai-api-key-here` with your actual OpenAI API key
3. The `appsettings.Development.json` file is ignored by git for security

### 3. Verify Configuration
Run the application and visit:
```
GET /api/config/status
```

This will show your configuration status without exposing sensitive data.

## File Structure
- `appsettings.json` - Default configuration (committed to git)
- `appsettings.Development.json` - Local development settings (git ignored)
- `Documents/` - Directory for txt files to be processed by Semantic Kernel
- `chatbot.db` - SQLite database for chat history (auto-created)