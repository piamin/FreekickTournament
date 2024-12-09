mergeInto(LibraryManager.library, {
    SaveScoreToServer: function(score) {
        console.log("Save, score: " + score);

        const urlParams = new URLSearchParams(window.location.search);
        const gameID = urlParams.get('gameId');
        const memberID = urlParams.get('memberId');
        
        //const gameID = "q13nq3m5"
        //const playerID = "6z5xpoe1"
        
        if (gameID && memberID)
        {
           //var url = "https://provider-api.tiktrix.gg/games/" + gameID + "/scores/save"; //live
           var url = "https://dev-provider-api.tiktrix.gg/games/" + gameID + "/scores/save"; //dev
           //console.log(url);
        
           var jsonParam = {
              "memberId" : memberID,
              "score" : Math.round(score)
           };
        
           const bodyText = JSON.stringify( jsonParam );
           //console.log(bodyText);
        
           window.fetch(url, {
              method: "POST",
              headers: {
                 "Content-Type": "application/json;charset=utf-8",
                 //"api-key": "provider_m42e2f8f_2ee976d69d13439ba9eaae24bed97f93" //live
               "api-key": "provider_m3quukrm_fb42342f3ccf41e791c813e6eb34ecd3" //dev
              },
              body: bodyText
           })
           .then( (response) => {
              console.log("response.status =", response.status);
              console.log("response.body =", response.body);
           });
        }        
    },
})
