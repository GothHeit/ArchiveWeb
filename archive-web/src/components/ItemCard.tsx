import { Link } from 'react-router-dom'
import type { FashionItemResponse } from '../types'
import { useAuth } from '../context/AuthContext'

interface Props {
  item: FashionItemResponse
  inWishlist?: boolean
  onToggleWishlist?: (item: FashionItemResponse) => void
}

export default function ItemCard({ item, inWishlist, onToggleWishlist }: Props) {
  const { user } = useAuth()

  const price = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: item.currency || 'BRL',
  }).format(item.currentPrice)

  return (
    <div className="group relative bg-surface border border-border hover:border-cream/20 transition-all duration-300">
      <Link to={`/items/${item.id}`} className="block">
        <div className="aspect-[3/4] overflow-hidden bg-s2">
          {item.imageUrl ? (
            <img
              src={item.imageUrl}
              alt={item.name}
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center">
              <span className="font-display text-5xl tracking-widest text-cream/10">A</span>
            </div>
          )}
        </div>

        <div className="p-4">
          <p className="text-xs tracking-widest text-muted uppercase mb-1">{item.brand}</p>
          <p className="text-sm text-cream leading-snug mb-3 line-clamp-2">{item.name}</p>
          <p className="font-display text-lg tracking-wider text-cream">{price}</p>
          {item.category && (
            <p className="text-xs text-muted mt-1 uppercase tracking-wider">{item.category}</p>
          )}
        </div>
      </Link>

      {user && onToggleWishlist && (
        <button
          onClick={(e) => {
            e.preventDefault()
            onToggleWishlist(item)
          }}
          className={`absolute top-3 right-3 w-8 h-8 flex items-center justify-center border text-sm transition-all duration-200 ${
            inWishlist
              ? 'border-cream bg-cream text-bg'
              : 'border-border bg-surface/80 text-muted hover:border-cream hover:text-cream opacity-0 group-hover:opacity-100'
          }`}
          title={inWishlist ? 'Remover da wishlist' : 'Adicionar à wishlist'}
        >
          {inWishlist ? '♥' : '♡'}
        </button>
      )}
    </div>
  )
}
