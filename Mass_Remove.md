## Mass remove Steam Demos/Free games
If you've activated a ton of demos for Steam sales, or anything else, this quick guide will help you automatically remove all of them really quickly! Just +- a second each.

#### Video Guide
<p align="center">
	<a href="https://youtu.be/bikDH7DQKgs">
	    <img alt="Website" src="https://i.imgur.com/vwGmRBX.png" target="_blank" height=300">
	</a>
</p>
												
### 1: Open Steam and the Console
Head to https://store.steampowered.com/account/licenses/.
Hit `Ctrl+Shift+I`, `F12` or Right-Click and choose Inspect.
Head to the Console tab.

### 2: Collecting AppIds
Either enter 
```js
var appIds = [<<<App ids list here>>>];
```
and add your comma seperated appIds list between the square brackets, or enter and run the following command to collect AppIds from the page that contain "Demo", "Trailer", "Teaser", "Cinematic", "Pegi" or "ESRB".
```js
// Thanks to https://gist.github.com/retvil/aa10748c31be44fe2b8b for the REGEX
// By: https://youtube.com/TroubleChute
var appIds = [];
var  rows = document.getElementsByClassName("account_table")[0].rows;
i = 0;
for (let row of rows){
    var cell = row.cells[1];
    if (/\b(?:trailer|teaser|demo|cinematic|pegi|esrb)\b/i.test(cell.textContent)) {
        packageId = /javascript:\s*RemoveFreeLicense\s*\(\s*(\d+)/.exec(cell.innerHTML);

        if (packageId !== null) {
            // By: https://tcno.co/
            i++;
            console.log(`[${i}] Removing: ${packageId[1]} - ${cell.innerHTML.split("</div>")[1].trim()}`);
            if (!appIds.includes(packageId[1]))appIds.push(packageId[1]);
        }
    }
}
```

### 3: Removing games automatically
Clicking the Remove button takes a lot of time. Instead, once you have populated `appIds`, run the following command to remove all the AppIds from the list from your account.
```js
// By: https://youtube.com/TroubleChute
function removeNextPackage(appIds, i) {
    if (i >= appIds.length) {
        console.log("Removed all AppIds from account.");
        return;
    }

    fetch("https://store.steampowered.com/account/removelicense", {
        "headers": {
            "accept": "*/*",
            "accept-language": "en-US,en;q=0.9",
            "content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            "sec-ch-ua": "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"102\", \"Google Chrome\";v=\"102\"",
            "sec-ch-ua-mobile": "?0",
            "sec-ch-ua-platform": "\"Windows\"",
            "sec-fetch-dest": "empty",
            "sec-fetch-mode": "cors",
            "sec-fetch-site": "same-origin",
            "x-requested-with": "XMLHttpRequest"
        },
        "referrer": "https://store.steampowered.com/account/licenses/",
        "referrerPolicy": "strict-origin-when-cross-origin",
        "body": `sessionid=${encodeURIComponent(window.g_sessionID)}&packageid=${appIds[i]}`,
        "method": "POST",
        "mode": "cors",
        "credentials": "include"

//Moved going to next id if successful on this one
    }).then((response) => {
        if (response.status !== 200) {
            console.log(`Error: ${response.status} - ${response.statusText}`);
            return;
        }
        return response.json();
// Added Check for ratelimiting
    }).then((data) => {
        if (data && data.success === 84) {
            console.log(`Rate limit exceeded. Retrying after delay...`);
            setTimeout(() => removeNextPackage(appIds, i), 600000); // Retry after 10 mins
        } else {
            console.log(`Removed: ${appIds[i]} (${i + 1}/${appIds.length})`);
            removeNextPackage(appIds, ++i);
        }
    }).catch((error) => {
        console.log(`Network error: ${error}`);
        setTimeout(() => removeNextPackage(appIds, i), 60000); // Retry after 60 seconds on network error
    });
}

removeNextPackage(appIds, 0);
```

### NO APPS REMOVED??!?!?!
Just hit `Ctrl+F5` to refresh the page, not keeping cache. You should see demos dissapear (`Ctrl+F` and search for ` Demo` to see how many vanished), or check the Steam client to see them dissapear in near realtime.

### Error?!!?!?
Just refresh the page, clearing cache with `Ctrl+F5`, copy both the commands back in and run it again -- or hit the Up arrow to get back to what you typed earlier, and run them again.
