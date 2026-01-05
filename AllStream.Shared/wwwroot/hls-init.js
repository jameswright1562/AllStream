window.initHlsPlayer = function (videoId, url) {
  var video = document.getElementById(videoId);
  if (!video || !url) return;
  if (window.Hls && window.Hls.isSupported()) {
    var hls = new window.Hls({ enableWorker: true });
    hls.loadSource(url);
    hls.attachMedia(video);
  } else if (video.canPlayType('application/vnd.apple.mpegURL')) {
    video.src = url;
  } else {
    video.src = url;
  }
};


window.playerFullscreen = () => {
    const el = document.getElementsByName("player")[0] || document.documentElement;

    const req =
        el.requestFullscreen ||
        el.webkitRequestFullscreen ||
        el.msRequestFullscreen;

    if (req && !document.fullscreenElement) req.call(el);
};
