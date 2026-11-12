# ESEP webhooks

AWS Lambda starter for Assignment 8. The function currently accepts a JSON string and returns its uppercase equivalent. The GitHub-to-Slack integration has not been implemented yet.

## Build

Requires the .NET 10 SDK.

```sh
dotnet build
```

## Deploy the starter

Install `Amazon.Lambda.Tools` and sign in to your local AWS profile before deploying. Deployment defaults use `us-east-2`. Select your profile on the command line, replacing `your-profile` with its local name.

```sh
dotnet lambda deploy-function EsepWebhook --profile your-profile
```

If `dotnet lambda` is not found, add the global tools directory to your shell's PATH:

```sh
export PATH="$PATH:$HOME/.dotnet/tools"
```

Test the deployed starter with the JSON string `"test this"`. It should return `"TEST THIS"`.

The completed integration will read its Slack webhook URL from the Lambda environment variable `SLACK_URL`. Keep the URL out of source files, deployment defaults, documentation, and Git history.
