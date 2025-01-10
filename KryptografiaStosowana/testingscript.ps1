$uri = 'http://localhost:5008/Wallet/callerAddress'
$address = [System.Web.HttpUtility]::UrlEncode('http://localhost:8080/')
$uri = "$uri`?address=$address"
$params = @{
    Uri = $uri
    Method = 'POST'
    Body = ''
    ContentType = 'application/json'
    Headers = @{
        'accept' = '*/*'
    }
}

$response = Invoke-RestMethod @params

$uri = 'http://localhost:5008/Wallet/addKey'
$privateKey = '51de9696926f38d48f58ed6017b3e31faaa9bf3125453c6d1311aabace37c7f8'
$uri = "$uri`?privateKey=$privateKey"
$params = @{
    Uri = $uri
    Method = 'POST'
    Body = ''
    ContentType = 'application/json'
    Headers = @{
        'accept' = 'text/plain'
    }
}

$response = Invoke-RestMethod @params

# $uri = 'http://localhost:5008/Wallet/createTransaction'
# $body = @{
#     address = '0259579f805a14cb86276c167d5e8fc737cd7d640e4850c3c94ec79337ee1c53e2'
#     amount = 2
# } | ConvertTo-Json

# $params = @{
#     Uri = $uri
#     Method = 'POST'
#     Body = $body
#     ContentType = 'application/json'
#     Headers = @{
#         'accept' = 'text/plain'
#     }
# }

# $response = Invoke-RestMethod @params

# $uri = 'http://localhost:8080/Peer/BroadcastOnOff'
# $params = @{
#     Uri = $uri
#     Method = 'GET'
#     Headers = @{
#         'accept' = '*/*'
#     }
# }

# $response = Invoke-RestMethod @params

# $uri = 'http://localhost:8081/Peer/BroadcastOnOff'
# $params = @{
#     Uri = $uri
#     Method = 'GET'
#     Headers = @{
#         'accept' = '*/*'
#     }
# }

# $response = Invoke-RestMethod @params

