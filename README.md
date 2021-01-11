# Reuters Top News
View [Reuters top news](https://www.reuters.com/news/archive/newsOne) in command line.
Rendering the output with [glow](https://github.com/charmbracelet/glow) will give you a better reading experience.
## Known issues
If there is an image defined inside block of `<p>`, it's code will be shown rawly just like below:
```
This is the content (<a href="link">img</a>).
```
If you render the output with `glow`, this will be tolerable:
```
This is the content (img).
```
