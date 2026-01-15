#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import axios from "axios";
import fs from "fs/promises";

const PYTHON_BACKEND_URL = "http://localhost:5000";

/**
 * MCP Server for Claude Voice transcription
 * Exposes voice transcription capabilities to Claude Code
 */
class ClaudeVoiceMCPServer {
  constructor() {
    this.server = new Server(
      {
        name: "claude-voice",
        version: "1.0.0",
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.setupHandlers();
    this.server.onerror = (error) => console.error("[MCP Error]", error);
    process.on("SIGINT", async () => {
      await this.server.close();
      process.exit(0);
    });
  }

  setupHandlers() {
    // List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: "transcribe_audio",
          description:
            "Transcribe an audio file to text using local Whisper model. Returns the transcribed text with word-level timestamps and confidence scores.",
          inputSchema: {
            type: "object",
            properties: {
              audio_path: {
                type: "string",
                description: "Path to the audio file to transcribe (WAV, MP3, etc.)",
              },
            },
            required: ["audio_path"],
          },
        },
        {
          name: "get_transcription_status",
          description: "Check if the transcription service is running and available",
          inputSchema: {
            type: "object",
            properties: {},
          },
        },
      ],
    }));

    // Handle tool calls
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        if (name === "transcribe_audio") {
          return await this.transcribeAudio(args.audio_path);
        } else if (name === "get_transcription_status") {
          return await this.getStatus();
        } else {
          throw new Error(`Unknown tool: ${name}`);
        }
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Error: ${error.message}`,
            },
          ],
          isError: true,
        };
      }
    });
  }

  async transcribeAudio(audioPath) {
    try {
      // Check if file exists
      await fs.access(audioPath);

      // Read the audio file
      const audioBuffer = await fs.readFile(audioPath);

      // Create form data
      const FormData = (await import("form-data")).default;
      const formData = new FormData();
      formData.append("audio", audioBuffer, {
        filename: "audio.wav",
        contentType: "audio/wav",
      });

      // Send to Python backend
      const response = await axios.post(
        `${PYTHON_BACKEND_URL}/transcribe`,
        formData,
        {
          headers: formData.getHeaders(),
          maxContentLength: Infinity,
          maxBodyLength: Infinity,
        }
      );

      const { text, words, language, duration } = response.data;

      // Format the response
      let result = `Transcription (${language}, ${duration.toFixed(1)}s):\n\n${text}\n\n`;

      // Add word details if available
      if (words && words.length > 0) {
        result += "Word-level details:\n";
        const lowConfidenceWords = words.filter((w) => w.confidence < 0.7);
        if (lowConfidenceWords.length > 0) {
          result += `\nLow confidence words (${lowConfidenceWords.length}):\n`;
          lowConfidenceWords.forEach((w) => {
            result += `- "${w.word}" (${(w.confidence * 100).toFixed(0)}% confidence at ${w.start.toFixed(1)}s)\n`;
          });
        }
      }

      return {
        content: [
          {
            type: "text",
            text: result,
          },
        ],
      };
    } catch (error) {
      if (error.code === "ENOENT") {
        throw new Error(`Audio file not found: ${audioPath}`);
      } else if (error.response) {
        throw new Error(
          `Transcription service error: ${error.response.data.error || error.message}`
        );
      } else {
        throw new Error(`Failed to transcribe: ${error.message}`);
      }
    }
  }

  async getStatus() {
    try {
      const response = await axios.get(`${PYTHON_BACKEND_URL}/health`, {
        timeout: 2000,
      });

      return {
        content: [
          {
            type: "text",
            text: `Transcription service is online.\nModel: ${response.data.model}\nStatus: ${response.data.status}`,
          },
        ],
      };
    } catch (error) {
      throw new Error(
        "Transcription service is offline. Please start the Claude Voice app."
      );
    }
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error("Claude Voice MCP server running on stdio");
  }
}

// Start the server
const server = new ClaudeVoiceMCPServer();
server.run().catch(console.error);
