<?php
$ch = curl_init();
$parameters = array(
    'apikey' => 'eda627d51969272535da5f7027d870c9',
    'number' => '09303238796',
    'message' => 'I just sent my first message with Semaphore',
    'sendername' => 'SEMAPHORE'
);

curl_setopt($ch, CURLOPT_URL, 'https://semaphore.co/api/v4/messages');
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

curl_setopt($ch, CURLOPT_POST, 1);
curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query($parameters));

curl_setopt($ch, CURLOPT_HTTPHEADER, array(
    'Content-Type: application/x-www-form-urlencoded',
));

$output = curl_exec($ch);

if ($output === false) {
    echo "cURL Error: " . curl_error($ch);
} else {
    echo "Message sent successfully!";
}
curl_close($ch);
?>
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Semaphore SMS API</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f4f4f9;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }

        .container {
            background-color: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            width: 100%;
            max-width: 600px;
            text-align: center;
        }

        h1 {
            color: #333;
        }

        .output {
            margin-top: 20px;
            padding: 15px;
            background-color: #f9f9f9;
            border: 1px solid #ddd;
            border-radius: 5px;
            color: #333;
            font-size: 14px;
            white-space: pre-wrap;
        }

        .btn {
            margin-top: 20px;
            padding: 10px 15px;
            background-color: #4CAF50;
            color: white;
            border: none;
            border-radius: 5px;
            cursor: pointer;
        }

        .btn:hover {
            background-color: #45a049;
        }
    </style>
</head>

<body>
    <div class="container">
        <h1>SMS Sent Successfully!</h1>
        <p>Your message was sent to the number <strong>09303238796</strong> via Semaphore API.</p>
        <div class="output">
            <h3>API Response:</h3>
            <pre><?php echo htmlspecialchars($output); ?></pre>
        </div>
        <button class="btn" onclick="window.location.reload();">Send Another Message</button>
    </div>
</body>

</html>