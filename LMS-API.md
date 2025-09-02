## Quick ping

This call will list the models that are loaded and ready.

```bash
curl http://127.0.0.1:1234/v1/models/
```

It is also a useful way to verify the server is reachable.

If you're on Windows and using Powershell, you can run this instead:

```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:1234/v1/models/"
```

This PowerShell command performs the same function as the curl command, sending a GET request to the specified endpoint
and returning the list of available models.

## LMS API

- Chat Completions
  This is an OpenAI-like call to the /v1/chat/completion endpoint using the curl utility.
  To run it on Mac or Linux, use any terminal. On Windows, use

```curl
curl http://127.0.0.1:1234/v1/chat/completions \
-H "Content-Type: application/json" \
-d '{
"model": "qwen/qwen3-4b-thinking-2507",
"messages": [
{ "role": "system", "content": "Always answer in rhymes." },
{ "role": "user", "content": "Introduce yourself." }
],
"temperature": 0.7,
"max_tokens": -1,
"stream": true
}'
```

The call is 'stateless', which means the server does not retain the conversation history. It is the caller's
responsibility to provide the whole conversation history in every call.

Streaming vs. accumulating a full response
Notice the "stream": true parameter. When set to true, LM Studio will stream back tokens as they are predicted.

If this parameter is set to false, the full prediction will be accumulated before the call returns. For longer
generations or slower models this might take a while!

## Structured Output

The API supports structured JSON outputs through the /v1/chat/completions endpoint when given a

. Doing this will cause the LLM to respond in valid JSON conforming to the schema provided.

It follows the same format as OpenAI's recently announced

API and is expected to work via the OpenAI client SDKs.

Example using curl
This example demonstrates a structured output request using the curl utility.

To run this example on Mac or Linux, use any terminal. On Windows, use

```curl
curl http://127.0.0.1:1234/v1/chat/completions \
-H "Content-Type: application/json" \
-d '{
"model": "qwen/qwen3-4b-thinking-2507",
"messages": [
{
"role": "system",
"content": "You are a helpful jokester."
},
{
"role": "user",
"content": "Tell me a joke."
}
],
"response_format": {
"type": "json_schema",
"json_schema": {
"name": "joke_response",
"strict": "true",
"schema": {
"type": "object",
"properties": {
"joke": {
"type": "string"
}
},
"required": ["joke"]
}
}
},
"temperature": 0.7,
"max_tokens": 50,
"stream": false
}'
```

All parameters recognized by /v1/chat/completions will be honored, and the JSON schema should be provided in the
json_schema field of response_format.

The JSON object will be provided in string form in the typical response field, choices[0].message.content, and will need
to be parsed into a JSON object.

Important: Not all models are capable of structured output, particularly LLMs below 7B parameters.

Check the model card if you are unsure if the model supports structured output.

Tool Use
What really is "tool use"?
Tool use describes:

LLMs output text requesting functions to be called (LLMs cannot directly execute code)
Your code executes those functions
Your code feeds the results back to the LLM.
Important
The model selected for tool use will greatly impact performance.

Some general guidance when selecting a model:

Not all models are capable of intelligent tool use
Bigger is better (i.e., a 7B parameter model will generally perform better than a 3B parameter model)
We've observed

to perform well in a wide variety of cases
This guidance may change
Example using Python
Below is a Python chatbot example that uses tools through OpenAI-like calls to the chat.completions API, enabling an LM
Studio model to query wikipedia.

To run:

Pre-requisites:

Install Python on your machine (

)
Install the OpenAI Python package (used only as a wrapper for LM Studio) with pip install openai
Steps:

Load a model
Start the server
Save the below example code with the "Save as Python File" button
Copy and paste the post-save command into your terminal and press Enter
tool-use-example.py

Save as Python file for a copy-and-paste command
Show Full Code (267 lines)
"""
LM Studio Tool Use Demo: Wikipedia Querying Chatbot
Demonstrates how an LM Studio model can query Wikipedia
"""

# Standard library imports

import itertools
import json
import shutil
import sys
import threading
import time
import urllib.parse
import urllib.request

# Third-party imports

from openai import OpenAI

# Initialize LM Studio client

client = OpenAI(base_url="http://127.0.0.1:1234/v1", api_key="lm-studio")
MODEL = "qwen/qwen3-4b-thinking-2507"

def fetch_wikipedia_content(search_query: str) -> dict:
"""Fetches wikipedia content for a given search_query"""
try:

# Search for most relevant article

```powershell
search_url = "https://en.wikipedia.org/w/api.php"
search_params = {
"action": "query",
"format": "json",
"list": "search",
"srsearch": search_query,
"srlimit": 1,
}

        url = f"{search_url}?{urllib.parse.urlencode(search_params)}"
        with urllib.request.urlopen(url) as response:
            search_data = json.loads(response.read().decode())

        if not search_data["query"]["search"]:
            return {
                "status": "error",
                "message": f"No Wikipedia article found for '{search_query}'",
            }

        # Get the normalized title from search results
        normalized_title = search_data["query"]["search"][0]["title"]

        # Now fetch the actual content with the normalized title
        content_params = {
            "action": "query",
            "format": "json",
            "titles": normalized_title,
            "prop": "extracts",
            "exintro": "true",
            "explaintext": "true",
            "redirects": 1,
        }

        url = f"{search_url}?{urllib.parse.urlencode(content_params)}"
        with urllib.request.urlopen(url) as response:
            data = json.loads(response.read().decode())

        pages = data["query"]["pages"]
        page_id = list(pages.keys())[0]

        if page_id == "-1":
            return {
                "status": "error",
                "message": f"No Wikipedia article found for '{search_query}'",
            }

        content = pages[page_id]["extract"].strip()
        return {
            "status": "success",
            "content": content,
            "title": pages[page_id]["title"],
        }

    except Exception as e:
        return {"status": "error", "message": str(e)}
```

# Define tool for LM Studio

WIKI_TOOL = {
"type": "function",
"function": {
"name": "fetch_wikipedia_content",
"description": (
"Search Wikipedia and fetch the introduction of the most relevant article. "
"Always use this if the user is asking for something that is likely on wikipedia. "
"If the user has a typo in their search query, correct it before searching."
),
"parameters": {
"type": "object",
"properties": {
"search_query": {
"type": "string",
"description": "Search query for finding the Wikipedia article",
},
},
"required": ["search_query"],
},
},
}

# Class for displaying the state of model processing

class Spinner:

```python
def __init__(self, message="Processing..."):
self.spinner = itertools.cycle(["-", "/", "|", "\\"])
self.busy = False
self.delay = 0.1
self.message = message
self.thread = None

    def write(self, text):
        sys.stdout.write(text)
        sys.stdout.flush()

    def _spin(self):
        while self.busy:
            self.write(f"\r{self.message} {next(self.spinner)}")
            time.sleep(self.delay)
        self.write("\r\033[K")  # Clear the line

    def __enter__(self):
        self.busy = True
        self.thread = threading.Thread(target=self._spin)
        self.thread.start()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.busy = False
        time.sleep(self.delay)
        if self.thread:
            self.thread.join()
        self.write("\r")  # Move cursor to beginning of line


def chat_loop():
"""
Main chat loop that processes user input and handles tool calls.
"""
messages = [
{
"role": "system",
"content": (
"You are an assistant that can retrieve Wikipedia articles. "
"When asked about a topic, you can retrieve Wikipedia articles "
"and cite information from them."
),
}
]

    print(
        "Assistant: "
        "Hi! I can access Wikipedia to help answer your questions about history, "
        "science, people, places, or concepts - or we can just chat about "
        "anything else!"
    )
    print("(Type 'quit' to exit)")

    while True:
        user_input = input("\nYou: ").strip()
        if user_input.lower() == "quit":
            break

        messages.append({"role": "user", "content": user_input})
        try:
            with Spinner("Thinking..."):
                response = client.chat.completions.create(
                    model=MODEL,
                    messages=messages,
                    tools=[WIKI_TOOL],
                )

            if response.choices[0].message.tool_calls:
                # Handle all tool calls
                tool_calls = response.choices[0].message.tool_calls

                # Add all tool calls to messages
                messages.append(
                    {
                        "role": "assistant",
                        "tool_calls": [
                            {
                                "id": tool_call.id,
                                "type": tool_call.type,
                                "function": tool_call.function,
                            }
                            for tool_call in tool_calls
                        ],
                    }
                )

                # Process each tool call and add results
                for tool_call in tool_calls:
                    args = json.loads(tool_call.function.arguments)
                    result = fetch_wikipedia_content(args["search_query"])

                    # Print the Wikipedia content in a formatted way
                    terminal_width = shutil.get_terminal_size().columns
                    print("\n" + "=" * terminal_width)
                    if result["status"] == "success":
                        print(f"\nWikipedia article: {result['title']}")
                        print("-" * terminal_width)
                        print(result["content"])
                    else:
                        print(
                            f"\nError fetching Wikipedia content: {result['message']}"
                        )
                    print("=" * terminal_width + "\n")

                    messages.append(
                        {
                            "role": "tool",
                            "content": json.dumps(result),
                            "tool_call_id": tool_call.id,
                        }
                    )

                # Stream the post-tool-call response
                print("\nAssistant:", end=" ", flush=True)
                stream_response = client.chat.completions.create(
                    model=MODEL, messages=messages, stream=True
                )
                collected_content = ""
                for chunk in stream_response:
                    if chunk.choices[0].delta.content:
                        content = chunk.choices[0].delta.content
                        print(content, end="", flush=True)
                        collected_content += content
                print()  # New line after streaming completes
                messages.append(
                    {
                        "role": "assistant",
                        "content": collected_content,
                    }
                )
            else:
                # Handle regular response
                print("\nAssistant:", response.choices[0].message.content)
                messages.append(
                    {
                        "role": "assistant",
                        "content": response.choices[0].message.content,
                    }
                )

        except Exception as e:
            print(
                f"\nError chatting with the LM Studio server!\n\n"
                f"Please ensure:\n"
                f"1. LM Studio server is running at 127.0.0.1:1234 (hostname:port)\n"
                f"2. Model '{MODEL}' is downloaded\n"
                f"3. Model '{MODEL}' is loaded, or that just-in-time model loading is enabled\n\n"
                f"Error details: {str(e)}\n"
                "See https://lmstudio.ai/docs/basics/server for more information"
            )
            exit(1)


if __name__ == "__main__":
chat_loop()
```

Start the server, then run the above script to chat with a model that is capable of querying wikipedia:

-> % python tool-use-example.py

Assistant: Hi! I can access Wikipedia to help answer your questions about history, science, people, places, or
concepts - or we can just chat about anything else!
(Type 'quit' to exit)

You:
Example using curl
This example demonstrates a model requesting a tool call using the curl utility.

To run this example on Mac or Linux, use any terminal. On Windows, use

```cmd
curl http://127.0.0.1:1234/v1/chat/completions \
-H "Content-Type: application/json" \
-d '{
"model": "qwen/qwen3-4b-thinking-2507",
"messages": [{"role": "user", "content": "What dell products do you have under $50 in electronics?"}],
"tools": [
{
"type": "function",
"function": {
"name": "search_products",
"description": "Search the product catalog by various criteria. Use this whenever a customer asks about product availability, pricing, or specifications.",
"parameters": {
"type": "object",
"properties": {
"query": {
"type": "string",
"description": "Search terms or product name"
},
"category": {
"type": "string",
"description": "Product category to filter by",
"enum": ["electronics", "clothing", "home", "outdoor"]
},
"max_price": {
"type": "number",
"description": "Maximum price in dollars"
}
},
"required": ["query"],
"additionalProperties": false
}
}
}
]
}'
All parameters recognized by /v1/chat/completions will be honored, and the array of available tools should be provided in the tools field.

If the model decides that the user message would be best fulfilled with a tool call, an array of tool call request objects will be provided in the response field, choices[0].message.tool_calls.

The finish_reason field of the top-level response object will also be populated with "tool_calls".

An example response to the above curl request will look like:
```json
{
"id": "chatcmpl-gb1t1uqzefudice8ntxd9i",
"object": "chat.completion",
"created": 1730913210,
"model": "qwen/qwen3-4b-thinking-2507",
"choices": [
{
"index": 0,
"logprobs": null,
"finish_reason": "tool_calls",
"message": {
"role": "assistant",
"tool_calls": [
{
"id": "365174485",
"type": "function",
"function": {
"name": "search_products",
"arguments": "{\"query\":\"dell\",\"category\":\"electronics\",\"max_price\":50}"
}
}
]
}
}
],
"usage": {
"prompt_tokens": 263,
"completion_tokens": 34,
"total_tokens": 297
},
"system_fingerprint": "qwen/qwen3-4b-thinking-2507"
}
```

In plain english, the above response can be thought of as the model saying:

"Please call the search_products function, with arguments:

'dell' for the query parameter,
'electronics' for the category parameter
'50' for the max_price parameter
and give me back the results"

The tool_calls field will need to be parsed to call actual functions/APIs, as shown in the Python example code.

```mermaid
┌──────────────────────────┐
│ SETUP: LLM + Tool list   │
└──────────┬───────────────┘
           ▼
┌──────────────────────────┐
│    Get user input        │◄────┐
└──────────┬───────────────┘     │
           ▼                     │
┌──────────────────────────┐     │
│ LLM prompted w/messages  │     │
└──────────┬───────────────┘     │
           ▼                     │
     Needs tools?                │
      │         │                │
    Yes         No               │
      │         │                │
      ▼         └────────────┐   │
┌─────────────┐              │   │
│Tool Response│              │   │
└──────┬──────┘              │   │
       ▼                     │   │
┌─────────────┐              │   │
│Execute tools│              │   │
└──────┬──────┘              │   │
       ▼                     ▼   │
┌─────────────┐          ┌───────────┐
│Add results  │          │  Normal   │
│to messages  │          │ response  │
└──────┬──────┘          └─────┬─────┘
       │                       ▲
       └───────────────────────┘
```

Tool List and Functions
It is critical to:

Write functions for the model to use
Provide a list of these functions and their parameters to the model through the tools field of the /v1/chat/completions
request body
In Python, for example:

# Function definition

def search_products(query: str, category: str = None, max_price: float = None):
"""
Search products in catalog based on criteria
Args:
query: Search terms
category: Product category filter
max_price: Maximum price filter
Returns:
Search results
"""

# Implementation here

pass

# Corresponding Tool List Definition

```json
tools = [
{
"type": "function",
"function": {
"name": "search_products",
"description": "Search the product catalog by various criteria",
"parameters": {
"type": "object",
"properties": {
"query": {
"type": "string",
"description": "Search terms or product name"
},
"category": {
"type": "string",
"description": "Product category to filter by",
"enum": ["electronics", "clothing", "home", "outdoor"]
},
"max_price": {
"type": "number",
"description": "Maximum price in dollars"
}
},
"required": ["query"]
}
}
}
]
```

General Usage Flow:

Send tools list with API request
Parse model's tool_calls response
Execute matching function with provided arguments
Return results to model
Online Documentation
For more in-depth documentation, visit the

## 2025-08-22 00:57:53  [INFO] [google/gemma-3-1b] Generated prediction:

```json
{
  "id": "chatcmpl-gv963ie1y4glqphlkiwjac",
  "object": "chat.completion",
  "created": 1755849472,
  "model": "google/gemma-3-1b",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello there! It’s great to be here. How can I help you today? Just let me know what you need – whether it's assisting with code, explaining concepts, or just offering some friendly conversation! 😊",
        "reasoning_content": "",
        "tool_calls": []
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 25,
    "completion_tokens": 46,
    "total_tokens": 71
  },
  "stats": {},
  "system_fingerprint": "google/gemma-3-1b"
}
```

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GemmaChatCompletion
{
    public class Root
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }

        [JsonPropertyName("stats")]
        public Stats Stats { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message Message { get; set; }

        [JsonPropertyName("logprobs")]
        public object Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("reasoning_content")]
        public string ReasoningContent { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<object> ToolCalls { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class Stats
    {
        // Empty object in your JSON, keep placeholder for future fields
    }
}
```