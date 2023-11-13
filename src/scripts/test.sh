#!/bin/bash

# Print the menu
echo "===================================="
echo "Choose a target environment (1-3): "
echo "===================================="
options=(
  "Development"
  "Production"
  "Quit"
)
name=""
# Select an option
COLUMNS=0
select option in "${options[@]}"; do
  case $option in
    "Development")
      url='http://localhost:8000/openai/chat'
      break
    ;;
    "Production")
      url='https://BetaOpenAIhttp.babosbird.com/openai/chat'
      break
    ;;
    "Quit")
      exit
    ;;
    *) echo "invalid option $REPLY" ;;
  esac
done

# Function to escape special characters in a JSON string
escape_json() {
  local json_string="$1"
  local escaped_string

  # Perform the necessary escaping of special characters
  escaped_string=$(echo "$json_string" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/\//\\\//g' -e 's/\n/\\n/g' -e 's/\r/\\r/g' -e 's/\t/\\t/g')

  # Return the escaped JSON string
  echo "$escaped_string"
}

# Array of tenants
tenants=("contoso" "fabrikam" "acme")
cities=("Pisa" "Lucca" "Florence" "Siena" "Carrara" "Livorno" "Prato" "Arezzo" "Massa" "Pistoia" "Grosseto" "Milano" "Monza" "Brescia" "Bergamo" "Mantova" "Parma")
questions=("tell me about the city" "could you please tell me more about the history of this city? Tell me about famous people who were born in the city." "I'd like to make a tour of the city, can you suggest any other nice town or historical place to visit nearby?")

# Configurable amount of calls to send
amount_of_calls=1000

# Loop through the amount of calls to send
for ((i = 0; i < $amount_of_calls; i++)); do

  # Get a random tenant from the array
  rand_tenant=${tenants[$((RANDOM % ${#tenants[@]}))]}

  # Get a random city from the array
  rand_city=${cities[$((RANDOM % ${#cities[@]}))]}

  # Get a random question from the array
  rand_question="Regarding $rand_city, ${questions[$((RANDOM % ${#questions[@]}))]}"

  # Prepare the request body
  request_body='[
    {
      "role": "system",
      "content": "The assistant is helpful, creative, clever, and very friendly."
    },
    {
      "role": "user",
      "content": "'$rand_question'"
    }
]'

  # Send the POST request and capture the response status code
    curl -X 'POST' \
    -s \
    -o /dev/null \
    -H 'accept: */*' \
    -H 'Content-Type: application/json' \
    -H "X-Tenant: $rand_tenant" \
    -w "tenant: $rand_tenant city: $rand_city question: $rand_question $statusCode: %{http_code}\n" \
    -d "$request_body" \
    $url

done
