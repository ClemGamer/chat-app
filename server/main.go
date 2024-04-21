package main

import (
	"fmt"
	"net/http"

	"github.com/gin-gonic/gin"
)

var (
	hub *Hub
)

func main() {

	hub = NewHub()
	go hub.Run()

	g := gin.Default()
	g.GET("/ws/chat", func(c *gin.Context) {
		serveWs(hub, c)
	})

	server := &http.Server{
		Addr:    "localhost:8080",
		Handler: g,
	}

	err := server.ListenAndServe()
	if err != nil {
		fmt.Println(err)
	}
}
