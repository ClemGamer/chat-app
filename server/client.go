package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"strings"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/gorilla/websocket"
)

const (
	// Time allowed to write a message to the peer.
	writeWait = 10 * time.Second

	// Time allowed to read a message from the peer.
	pongWait = 60 * time.Second

	// Send pings to peer with this period. Must be less than pongWait.
	pingPeriod = pongWait * 9 / 10

	// Maximum message size allowed from peer.
	maxMessageSize = 512
)

var (
	newline = []byte("\n")
	space   = []byte(" ")
)

type Client struct {
	id   int
	conn *websocket.Conn
	send chan *WrapMessage
	hub  *Hub
}

// readPump pumps messages from websocket connection to the hub
func (c *Client) readPump() {
	defer func() {
		c.hub.unregister <- c
		c.conn.Close()
	}()
	c.conn.SetReadLimit(maxMessageSize)
	c.conn.SetReadDeadline(time.Now().Add(pongWait))
	c.conn.SetPongHandler(func(appData string) error {
		c.conn.SetReadDeadline(time.Now().Add(pongWait))
		return nil
	})
	for {
		_, message, err := c.conn.ReadMessage()
		if err != nil {
			if websocket.IsUnexpectedCloseError(err) {
				log.Printf("error: %v", err)
			}
			break
		}

		receivedMsg := &WrapMessage{}
		if err = json.Unmarshal(message, receivedMsg); err != nil {
			log.Printf("error: %v", err)
			continue
		}
		wrap := &WrapMessage{
			Protocal: receivedMsg.Protocal,
			ID:       c.id,
		}

		switch receivedMsg.Protocal {
		case "0001":
			userStrings := make([]string, 0, len(users))
			for k, v := range users {
				userStrings = append(userStrings, fmt.Sprintf("%v:%v", k, v))
			}
			wrap.Data = strings.Join(userStrings, ",")
			c.send <- wrap
		case "0002":
			message = bytes.TrimSpace(bytes.Replace([]byte(receivedMsg.Data.(string)), newline, space, -1))
			wrap.Data = string(message)
			// c.hub.broadcast <- message
			c.hub.broadcast <- wrap
		default:
			log.Printf("protocal is not defined: %v", receivedMsg.Protocal)
		}
	}
}

// writePump pumps messages from the hub to the websocket connection.
func (c *Client) writePump() {
	ticker := time.NewTicker(pingPeriod)
	defer func() {
		ticker.Stop()
		c.conn.Close()
	}()
	for {
		select {
		case message, ok := <-c.send:
			c.conn.SetWriteDeadline(time.Now().Add(writeWait))
			// channel closed
			if !ok {
				c.conn.WriteMessage(websocket.CloseMessage, []byte{})
			}

			w, err := c.conn.NextWriter(websocket.TextMessage)
			if err != nil {
				return
			}
			Write(w, message)

			n := len(c.send)
			for i := 0; i < n; i++ {
				// w.Write(newline)
				Write(w, <-c.send)
			}

			if err := w.Close(); err != nil {
				return
			}
		case <-ticker.C:
			c.conn.SetWriteDeadline(time.Now().Add(writeWait))
			if err := c.conn.WriteMessage(websocket.PingMessage, []byte{}); err != nil {
				return
			}
		}
	}
}

func Write(w io.WriteCloser, message interface{}) bool {
	m, err := json.Marshal(message)
	if err != nil {
		log.Printf("error: %v", err)
		return false
	}
	_, err = w.Write(m)
	if err != nil {
		log.Printf("error: %v", err)
		return false
	}
	return true
}

var upgrader = websocket.Upgrader{
	ReadBufferSize:  512,
	WriteBufferSize: 512,
}

var users = map[int]string{
	1: "Clement",
	2: "LockStar",
}

func serveWs(hub *Hub, ctx *gin.Context) {
	var q struct {
		ID int `form:"id" binding:"required"`
	}
	err := ctx.BindQuery(&q)
	if err != nil {
		log.Printf("error: %v", err)
		return
	}

	_, ok := users[q.ID]
	if !ok {
		log.Printf("user not found: %v", q.ID)
		return
	}

	ws, err := upgrader.Upgrade(ctx.Writer, ctx.Request, nil)
	if err != nil {
		log.Printf("error: %v", err)
		return
	}

	client := &Client{
		id:   q.ID,
		conn: ws,
		send: make(chan *WrapMessage),
		hub:  hub,
	}
	hub.register <- client

	go client.writePump()
	go client.readPump()
}
