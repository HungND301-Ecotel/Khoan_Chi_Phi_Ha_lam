# Copilot Instructions

## General Guidelines
- Always call detect_memories when user indicates coding standard or preference.
- Always check for errors before ending the conversation.

## Code Style
- Follow existing project coding style for all code changes.
- Adjust includes/projections rather than changing algorithmic logic.
- Export columns to match header text width.

## Project-Specific Rules
- User is working in Visual Studio 2026 on .NET 8 and needs API updates for plan vs adjustment data separation.
- APIs must accept adjustment output IDs while returning plan details using matching business conditions.
- For acceptance report item logs, fully-accounted items must still be displayed in the final accounting month and only disappear from the following month. In the final accounting period, TotalValueToAccount, OriginalValue-equivalent, ValueByStandard, and AccountedValueThisPeriod should align, and PendingValueEndPeriod should be 0.